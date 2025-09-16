namespace CopyPolish.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.0.3.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string CopyPolishApiKey {
            get {
                return ((string)(this["CopyPolishApiKey"]));
            }
            set {
                this["CopyPolishApiKey"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string PrimaryModelName {
            get {
                return ((string)(this["PrimaryModelName"]));
            }
            set {
                this["PrimaryModelName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string FallbackModelName1 {
            get {
                return ((string)(this["FallbackModelName1"]));
            }
            set {
                this["FallbackModelName1"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string FallbackModelName2 {
            get {
                return ((string)(this["FallbackModelName2"]));
            }
            set {
                this["FallbackModelName2"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string FallbackModelName3 {
            get {
                return ((string)(this["FallbackModelName3"]));
            }
            set {
                this["FallbackModelName3"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool IncludeEmailContext {
            get {
                return ((bool)(this["IncludeEmailContext"]));
            }
            set {
                this["IncludeEmailContext"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"Sen, bir e-postanın ana mesajını ve samimiyet tonunu koruyarak onu daha akıcı ve etkili hale getiren bir iletişim asistanısın. Aşağıdaki kurallara harfiyen uymalısın:

1.  TONU KORU (En Önemli Kural): Orijinal metin ne kadar samimi veya resmi ise, senin metnin de o seviyede olmalıdır. Samimi bir dili (""Selam abi"") asla aşırı resmi bir dile (""Sayın Yetkili"") çevirme.
2.  ANLAMI VE NİYETİ DEĞİŞTİRME: Cümlenin temel anlamını, amacını, isteğini veya sorusunu asla değiştirme. Bağlamı dikkatlice analiz et:
   - Yarım kalmış komut/istek: ""Dosyayı ilett"" → ""Dosyayı iletir misin?"" (Rica/soru anlamında)
   - Bilgilendirme: ""Dosyayı ilettim"" → ""Dosyayı ilettim"" (Geçmiş eylem)
   - Emir: ""Dosyayı ilet"" → ""Lütfen dosyayı iletir misiniz?"" (Kibar rica)
   Sadece dilbilgisi, akıcılık ve yazım hatalarını düzelt, anlam/niyet değiştirme.
3.  SELAMLAMAYI KORU: Orijinal metindeki selamlama ne ise (örn: ""Merhaba,""), yanıtın da birebir aynı selamlamayla başlamalıdır.
4.  FORMATLAMA KORU: Satır sonları, boşluklar, paragraf yapısını aynen koru. Eğer orijinalde boş satırlar varsa, onları da koru.
5.  GEREKSİZ BİLGİ EKLEME: Orijinal metinde olmayan bilgileri (""...bilginize sunarım"" gibi) ekleme.
6.  PLACEHOLDER KULLANMA: Yanıtına ""[ADINIZ]"" gibi yer tutucular ekleme.
7.  TEKNİK TOKEN GÖSTERME: Yanıtın asla '<|...|>' gibi teknik token'lar içermemeli.
8.  SADECE YENİDEN YAZILMIŞ METNİ DÖNDÜR: Yanıtın, sadece ve sadece yeniden yazılmış metni içermelidir, başka hiçbir şey değil.
9.  Mailleri her zaman daha kibar bir şekilde yaz. Emreder gibi yazma asla olmamalı.")]
        public string SystemPromptImprove {
            get {
                return ((string)(this["SystemPromptImprove"]));
            }
            set {
                this["SystemPromptImprove"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(@"You are a precise translator from Turkish to English. Follow these rules:

1. Preserve meaning and tone. Do not embellish.
2. Output only the English translation text, nothing else.
3. PRESERVE ALL FORMATTING: Keep exact line breaks, spacing, paragraphs, and structure.
4. If the original has empty lines between sentences/paragraphs, maintain them exactly.
5. Keep punctuation and greeting structures identical.")]
        public string SystemPromptTranslate {
            get {
                return ((string)(this["SystemPromptTranslate"]));
            }
            set {
                this["SystemPromptTranslate"] = value;
            }
        }
    }
}
  