### Known Issues & Fix Log

#### ClickOnce manifest path duplication blocks install {#clickonce-manifest-path-duplication}
- Date: 2025-11-25T11:57:30Z
- Context: unknown-branch/local-Windows/MSBuild-17.14
- Error signature: `DeploymentDownloadException: ...dist/Application Files/CopyPolish_1_0_0_5/Application Files/CopyPolish_1_0_0_5/CopyPolish.dll.manifest` not found
- Symptoms/Impact: VSTO add-in install fails; “Arama” butonu ve güncel eklenti yüklenemiyor.
- Root cause: ClickOnce publish çıktı yolunda “Application Files” klasörü iki kez yer alacak şekilde manifest/codebase oluşması (eski publish kalıntıları + yerel dosyadan kurulum).
- Resolution: dist temizlenip `BootstrapperEnabled=false` ile yeniden publish edilerek yeni `CopyPolish/dist/CopyPolish.vsto` üretildi; kurulum kök VSTO’dan çalıştırılıyor.
- Prevent recurrence:
  - Publish öncesi `dist` ve `bin/Release/app.publish` klasörlerini temizle.
  - ClickOnce kurulumunu her zaman kök `CopyPolish/dist/CopyPolish.vsto` üzerinden başlat, iç içe klasörlerden değil.
  - PublishDir/InstallUrl ayarlarını tek konuma sabitle, web bootstrapper kapalı tut.
- Files/Commands touched: `CopyPolish/CopyPolish.csproj` (publish ayarları), `CopyPolish/dist` (yeniden üretim), `MSBuild ... /t:Publish /p:BootstrapperEnabled=false`
- References: n/a

