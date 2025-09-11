import requests
import json
import logging
import keyring
import time
import os
from typing import Optional, Dict, Any, List
from src import config as cfg

SYSTEM_PROMPT = """Sen, bir e-postanın ana mesajını ve samimiyet tonunu koruyarak onu daha akıcı ve etkili hale getiren bir iletişim asistanısın. Aşağıdaki kurallara harfiyen uymalısın:

1.  TONU KORU (En Önemli Kural): Orijinal metin ne kadar samimi veya resmi ise, senin metnin de o seviyede olmalıdır. Samimi bir dili ("Selam abi") asla aşırı resmi bir dile ("Sayın Yetkili") çevirme.
2.  ANLAMI DEĞİŞTİRME: Cümlenin temel anlamını, amacını veya içerdiği komutu asla değiştirme. Sadece dilbilgisi, akıcılık ve yazım hatalarını düzelt. Örneğin, 'Dosyayı ilet' komutunu 'Dosyayı iletiyorum' ifadesine çevirme.
3.  SELAMLAMAYI KORU: Orijinal metindeki selamlama ne ise (örn: "Merhaba,"), yanıtın da birebir aynı selamlamayla başlamalıdır.
4.  GEREKSİZ BİLGİ EKLEME: Orijinal metinde olmayan bilgileri ("...bilginize sunarım" gibi) ekleme.
5.  PLACEHOLDER KULLANMA: Yanıtına "[ADINIZ]" gibi yer tutucular ekleme.
6.  TEKNİK TOKEN GÖSTERME: Yanıtın asla '<|...|>' gibi teknik token'lar içermemeli.
7.  SADECE YENİDEN YAZILMIŞ METNİ DÖNDÜR: Yanıtın, sadece ve sadece yeniden yazılmış metni içermelidir, başka hiçbir şey değil.
8.  Mailleri her zaman daha kibar bir şekilde yaz. Emreder gibi yazma asla olmamalı."""

TRANSLATE_PROMPT = """You are a precise translator from Turkish to English. Follow these rules:

1. Preserve meaning and tone. Do not embellish.
2. Output only the English translation text, nothing else.
3. Keep formatting and line breaks when possible.
"""


class APIHandler:
    def __init__(self):
        self.logger = logging.getLogger(__name__)
        self.base_url = "https://openrouter.ai/api/v1/chat/completions"
        self._apply_config(cfg.load_config())

    def _apply_config(self, c: Dict[str, Any]):
        self.timeout = int(c.get("timeout", 30))
        self.max_retries = int(c.get("max_retries", 2))
        self.model = c.get("model_improve", "qwen/qwen3-coder:free")
        self.model_improve = c.get("model_improve", "qwen/qwen3-coder:free")
        self.model_translate = c.get("model_translate", "qwen/qwen3-coder:free")
        self.model_translate_candidates: List[str] = list(c.get("translate_candidates", [])) or [self.model_translate]
        self.model_improve_candidates: List[str] = list(c.get("improve_candidates", [])) or [self.model_improve]
        self.auto_fallback = bool(c.get("auto_fallback", True))
        self.debug_http = bool(c.get("debug_http", False)) or bool(os.environ.get("COPY_POLISH_DEBUG_HTTP"))
        self.log_request_body = bool(c.get("log_request_body", False)) or bool(os.environ.get("COPY_POLISH_LOG_REQUEST_BODY"))

    def _refresh_config(self):
        try:
            self._apply_config(cfg.load_config())
        except Exception:
            pass

    def get_api_key(self) -> Optional[str]:
        try:
            api_key = keyring.get_password("CopyPolish", "openrouter_api_key")
            return api_key
        except Exception as e:
            self.logger.error(f"API anahtarı alınamadı: {e}")
            return None

    def set_api_key(self, api_key: str) -> bool:
        try:
            keyring.set_password("CopyPolish", "openrouter_api_key", api_key)
            return True
        except Exception as e:
            self.logger.error(f"API anahtarı kaydedilemedi: {e}")
            return False

    def improve_text(self, text: str) -> Optional[str]:
        self._refresh_config()
        if self.auto_fallback:
            return self._make_api_request_with_fallback(
                user_prompt=text,
                candidates=self.model_improve_candidates,
                system_prompt=SYSTEM_PROMPT
            )
        return self._make_api_request(
            user_prompt=text,
            system_prompt=SYSTEM_PROMPT,
            model=self.model_improve
        )

    def translate_text(self, text: str) -> Optional[str]:
        self._refresh_config()
        if self.auto_fallback:
            return self._make_api_request_with_fallback(
                user_prompt=text,
                candidates=self.model_translate_candidates,
                system_prompt=TRANSLATE_PROMPT
            )
        return self._make_api_request(
            user_prompt=text,
            system_prompt=TRANSLATE_PROMPT,
            model=self.model_translate
        )

    def _is_transient(self, status_code: int, body_text: str, error_message: Optional[str]) -> bool:
        t = (body_text or "") + " " + (error_message or "")
        t_low = t.lower()
        if status_code >= 500:
            return True
        keywords = [
            "upstream error", "model endpoint", "temporar", "timeout",
            "try again", "unavailable", "overload", "busy",
        ]
        return any(k in t_low for k in keywords)

    def _make_api_request(self, user_prompt: str, system_prompt: Optional[str] = None, api_key_override: Optional[str] = None, model: Optional[str] = None) -> Optional[str]:
        api_key = api_key_override if api_key_override else self.get_api_key()
        if not api_key:
            raise Exception("API anahtarı bulunamadı. Lütfen ayarlardan API anahtarınızı girin.")

        model_to_use = model or self.model

        headers = {
            "Authorization": f"Bearer {api_key}",
            "Content-Type": "application/json; charset=utf-8",
            "HTTP-Referer": "https://copypolish.app",
            "X-Title": "CopyPolish",
        }

        messages = []
        if system_prompt:
            messages.append({"role": "system", "content": system_prompt})
        messages.append({"role": "user", "content": user_prompt})

        data = {
            "model": model_to_use,
            "messages": messages,
            "max_tokens": 1000,
            "temperature": 0.3,
            "top_p": 0.9,
            "frequency_penalty": 0,
            "presence_penalty": 0,
        }

        if self.debug_http:
            safe_headers = dict(headers)
            if "Authorization" in safe_headers:
                safe_headers["Authorization"] = "Bearer ***"
            self.logger.info(f"HTTP POST {self.base_url} model={model_to_use} timeout={self.timeout}")
            try:
                self.logger.info(f"Headers: {json.dumps(safe_headers)}")
            except Exception:
                self.logger.info(f"Headers: {safe_headers}")
            if self.log_request_body:
                try:
                    self.logger.info(f"Body: {json.dumps(data, ensure_ascii=False)}")
                except Exception:
                    self.logger.info("Body: <unserializable>")

        for attempt in range(self.max_retries + 1):
            try:
                self.logger.info("API isteği gönderiliyor...")
                response = requests.post(
                    url=self.base_url,
                    headers=headers,
                    data=json.dumps(data, ensure_ascii=False).encode('utf-8'),
                    timeout=self.timeout,
                )

                status = response.status_code
                text_body = ''
                try:
                    text_body = response.text or ''
                except Exception:
                    text_body = ''

                self.logger.info(f"HTTP durum kodu: {status}")
                if self.debug_http:
                    try:
                        self.logger.info(f"Yanıt gövdesi: {text_body}")
                    except Exception:
                        pass

                if status == 200:
                    result = response.json()
                    if self.debug_http:
                        try:
                            self.logger.info(f"Yanıt JSON: {json.dumps(result, ensure_ascii=False)}")
                        except Exception:
                            self.logger.info("Yanıt JSON: <unserializable>")

                    if isinstance(result, dict) and result.get('error'):
                        err = result['error']
                        msg = err.get('message') if isinstance(err, dict) else str(err)
                        if self._is_transient(status, text_body, msg) and attempt < self.max_retries:
                            self.logger.warning(f"Geçici API hatası (deneme {attempt+1}): {msg}")
                            time.sleep(1.5 * (attempt + 1))
                            continue
                        raise Exception(msg)

                    content_text = None
                    if isinstance(result, dict):
                        choices = result.get('choices')
                        if isinstance(choices, list) and choices:
                            msg = choices[0].get('message', {})
                            if isinstance(msg, dict):
                                content = msg.get('content')
                                if isinstance(content, str):
                                    content_text = content.strip()

                    if content_text:
                        self.logger.info("API isteği başarılı (HTTP 200)")
                        return content_text
                    raise Exception("API yanıtında geçerli içerik bulunamadı")

                elif status == 401:
                    raise Exception("API anahtarı geçersiz. Lütfen ayarlardan kontrol edin.")
                elif status == 429:
                    raise Exception("API rate limit aşıldı. Lütfen daha sonra tekrar deneyin.")
                else:
                    if self._is_transient(status, text_body, None) and attempt < self.max_retries:
                        self.logger.warning(f"Geçici HTTP hata {status} (deneme {attempt+1}). Tekrar deneniyor...")
                        time.sleep(1.5 * (attempt + 1))
                        continue
                    raise Exception(f"API hatası (HTTP {status}): {text_body}")

            except requests.exceptions.Timeout:
                if attempt < self.max_retries:
                    self.logger.warning(f"API zaman aşımı (deneme {attempt+1}). Tekrar deneniyor...")
                    time.sleep(1.5 * (attempt + 1))
                    continue
                raise Exception("API isteği zaman aşımına uğradı")
            except requests.exceptions.RequestException as e:
                if attempt < self.max_retries:
                    self.logger.warning(f"API bağlantı hatası (deneme {attempt+1}). Tekrar deneniyor...")
                    time.sleep(1.5 * (attempt + 1))
                    continue
                raise Exception(f"API istek hatası: {str(e)}")

        raise Exception("Bilinmeyen API hatası")

    def _should_try_next_model(self, error_message: Optional[str]) -> bool:
        msg = (error_message or "").lower()
        if not msg:
            return False
        provider_err = [
            "upstream error", "model endpoint", "temporar",
            "unavailable", "overload", "busy", "http 404", 
            "no endpoints", "no endpoint", "endpoint not found",
        ]
        if any(k in msg for k in provider_err):
            return True
        if "api anahtarı" in msg or "geçersiz" in msg or "rate limit" in msg:
            return False
        return False

    def _make_api_request_with_fallback(self, user_prompt: str, candidates: list[str], system_prompt: Optional[str] = None, api_key_override: Optional[str] = None) -> Optional[str]:
        last_err: Optional[Exception] = None
        for idx, m in enumerate(candidates):
            try:
                self.logger.info(f"Model kullanılıyor: {m}")
                return self._make_api_request(
                    user_prompt=user_prompt,
                    system_prompt=system_prompt,
                    api_key_override=api_key_override,
                    model=m
                )
            except Exception as e:
                last_err = e
                if idx < len(candidates) - 1 and self._should_try_next_model(str(e)):
                    self.logger.warning(f"Model '{m}' başarısız: {e}. Sonraki model deneniyor...")
                    continue
                raise
        if last_err:
            raise last_err
        return None

    def test_api_connection(self) -> tuple[bool, str]:
        try:
            result = self._make_api_request(user_prompt="Test")
            if result:
                return True, "API bağlantısı başarılı"
            else:
                return False, "API'den geçersiz yanıt alındı"
        except Exception as e:
            return False, str(e)

    def test_api_connection_with_key(self, api_key: str) -> tuple[bool, str]:
        try:
            result = self._make_api_request(user_prompt="Test", api_key_override=api_key)
            if result:
                return True, "API bağlantısı başarılı"
            else:
                return False, "API'den geçersiz yanıt alındı"
        except Exception as e:
            return False, str(e)
  