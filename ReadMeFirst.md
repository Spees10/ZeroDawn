After Cloning give this prompt to the AI model:
-------------------------------------------------
استخدم المشروع الحالي كـ starter template، واعملي bootstrap كامل لمشروع جديد بالقيم التالية فقط، وغيّر كل الأماكن المرتبطة بها في الكود والـ config والـ docs والـ seeding والـ branding، من غير ما تسيب أي قيمة قديمة hardcoded.

ProjectName: <اسم المشروع الجديد>
SolutionName: <اسم الـ solution لو هيختلف>
AppDisplayName: <الاسم المعروض للمستخدم>
AppShortName: <الاختصار أو initials مثل ZD>
CompanyName: <اسم الشركة أو الجهة>
PublisherName: <اسم الـ publisher للويندوز/MSIX>
PackageIdentityName: <هوية التطبيق للويندوز>
AndroidApplicationId: <application id مثل com.company.app>
DefaultCulture: <ar أو en>
DefaultTheme: <light أو dark>

WebBaseUrl: <مثال https://localhost:7001>
ApiBaseUrl: <مثال https://localhost:7001>
HttpPort: <مثال 5000>
HttpsPort: <مثال 7001>
AllowedCorsOrigins: <origin1,origin2,origin3>

DatabaseProvider: <SqlServer أو غيره>
ConnectionString: <connection string الجديدة>
AutoMigrateOnStartup: <true أو false>

JwtSecret: <JWT secret جديدة قوية>
JwtIssuer: <issuer>
JwtAudience: <audience>
AccessTokenMinutes: <مثال 60>
RefreshTokenDays: <مثال 7>

SmtpHost: <smtp host>
SmtpPort: <smtp port>
SmtpUsername: <smtp username>
SmtpPassword: <smtp password>
SmtpFromEmail: <from email>
SmtpFromName: <from name>

RequireEmailConfirmation: <true أو false>
AllowSelfRegistration: <true أو false>
EnableSwaggerInDevelopment: <true أو false>

SeedSuperAdminEmail: <admin email الجديد>
SeedSuperAdminPassword: <admin password الجديد>
SeedSuperAdminFullName: <اسم الأدمن>
DisableLegacySeedAdmins: <true أو false>

SupportEmail: <support email>
ContactEmail: <contact email>
ContactPhone: <رقم التواصل إن وجد>
AboutText: <نبذة مختصرة عن التطبيق>

BrandPrimaryColor: <hex>
BrandSecondaryColor: <hex>
BrandAccentColor: <hex>
LogoStrategy: <text-only أو svg أو image-placeholder>

WindowsSigningCertificatePath: <path أو empty>
WindowsSigningCertificatePassword: <password أو empty>
UseMsixPackaging: <true أو false>

RemoveAllOldBranding: true
RemoveAllPersonalEmails: true
RemoveAllHardcodedSecrets: true
UpdateLocalizationResources: true
UpdateDocs: true
UpdateSeedData: true
UpdateLaunchSettings: true
UpdateManifestValues: true
UpdateEmailTemplates: true
UpdateHealthCheckBranding: true
KeepFeatureFlagsAsIs: <true أو false>

بعد تطبيق التغييرات:
1. اعمل list واضح بكل الملفات اللي اتغيرت.
2. نبهني لو فيه أي قيمة حساسة ماينفعش تتحط في appsettings أو في client.
3. طلعلي commands المطلوبة لـ User Secrets أو Environment Variables في سطور منفصلة.
4. لو فيه أي hardcoded value قديمة مازالت موجودة، اذكرها صراحة.

