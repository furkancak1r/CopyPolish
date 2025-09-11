import os
import json
from typing import Dict, Any


DEFAULT_CONFIG: Dict[str, Any] = {
    "model_improve": "qwen/qwen3-coder:free",
    "model_translate": "qwen/qwen3-coder:free",
    "auto_fallback": True,
    "translate_candidates": [
        "qwen/qwen3-coder:free",
        "mistralai/mistral-nemo:free",
        "qwen/qwen2.5:free",
        "meta-llama/llama-3.1-8b-instruct:free",
    ],
    "improve_candidates": [
        "qwen/qwen3-coder:free",
        "mistralai/mistral-nemo:free",
        "qwen/qwen2.5:free",
        "meta-llama/llama-3.1-8b-instruct:free",
    ],
    "timeout": 30,
    "max_retries": 2,
    # Debugging
    "debug_http": True,  # Tüm HTTP yanıt gövdesini logla (konsolda da görünür)
    "log_request_body": False,  # İstek gövdesini de loglamak için True yapın
}


def _config_dir() -> str:
    # Windows: use %APPDATA%\CopyPolish
    base = os.environ.get("APPDATA") if os.name == "nt" else os.path.expanduser("~")
    path = os.path.join(base, "CopyPolish")
    try:
        os.makedirs(path, exist_ok=True)
    except Exception:
        # Fallback to local folder
        path = os.path.abspath(".")
    return path


def get_config_path() -> str:
    return os.path.join(_config_dir(), "config.json")


def _merge_defaults(user_cfg: Dict[str, Any]) -> Dict[str, Any]:
    merged = dict(DEFAULT_CONFIG)
    merged.update(user_cfg or {})
    # Ensure candidates include selected defaults at front, unique order
    def _ensure_front(lst, item):
        out = [item] + [x for x in lst if x != item]
        return out
    merged["translate_candidates"] = _ensure_front(
        merged.get("translate_candidates", DEFAULT_CONFIG["translate_candidates"]),
        merged.get("model_translate", DEFAULT_CONFIG["model_translate"]),
    )
    merged["improve_candidates"] = _ensure_front(
        merged.get("improve_candidates", DEFAULT_CONFIG["improve_candidates"]),
        merged.get("model_improve", DEFAULT_CONFIG["model_improve"]),
    )
    return merged


def load_config() -> Dict[str, Any]:
    path = get_config_path()
    try:
        if os.path.exists(path):
            with open(path, "r", encoding="utf-8") as f:
                data = json.load(f)
            return _merge_defaults(data if isinstance(data, dict) else {})
    except Exception:
        pass
    return dict(DEFAULT_CONFIG)


def save_config(cfg: Dict[str, Any]) -> bool:
    try:
        path = get_config_path()
        with open(path, "w", encoding="utf-8") as f:
            json.dump(_merge_defaults(cfg), f, ensure_ascii=False, indent=2)
        return True
    except Exception:
        return False
