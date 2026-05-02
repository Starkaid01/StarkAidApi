import org.gradle.kotlin.dsl.implementation
import java.util.Properties

// Top-level variable for Room version
val room_version = "2.7.2"

val starkAidLocalProperties = Properties().apply {
    val configFile = rootProject.file("starkaid.local.properties")
    if (configFile.exists()) {
        configFile.inputStream().use(::load)
    }
}

fun configValue(name: String, defaultValue: String = ""): String {
    return starkAidLocalProperties.getProperty(name)
        ?: (findProperty(name) as String?)
        ?: System.getenv(name)
        ?: defaultValue
}

fun escapeBuildConfig(value: String): String {
    return value
        .replace("\\", "\\\\")
        .replace("\"", "\\\"")
}

val starkAidIsDevelopment = configValue("STARKAID_IS_DEVELOPMENT", "false").equals("true", ignoreCase = true)
val starkAidDevApiBaseUrl = configValue("STARKAID_DEV_API_BASE_URL", "http://localhost:5000")
val starkAidDevWebBaseUrl = configValue("STARKAID_DEV_WEB_BASE_URL", "http://localhost:5001")
val starkAidProdApiBaseUrl = configValue("STARKAID_PROD_API_BASE_URL", "https://starkaid.runasp.net")
val starkAidProdWebBaseUrl = configValue("STARKAID_PROD_WEB_BASE_URL", "https://starkaidautomacao.runasp.net")
val starkAidSpotifyClientId = configValue("STARKAID_SPOTIFY_CLIENT_ID", "CHANGE_ME")
val starkAidSpotifyClientSecret = configValue("STARKAID_SPOTIFY_CLIENT_SECRET", "CHANGE_ME")
val starkAidEwelinkClientId = configValue("STARKAID_EWELINK_CLIENT_ID", "CHANGE_ME")
val starkAidEwelinkClientSecret = configValue("STARKAID_EWELINK_CLIENT_SECRET", "CHANGE_ME")
val admobAppId = configValue("ADMOB_APP_ID", "ca-app-pub-0000000000000000~0000000000")
val unityAdsAppId = configValue("UNITY_ADS_APP_ID", "0000000")
val hasGoogleServicesJson = project.file("google-services.json").exists()

plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    id("org.jetbrains.kotlin.plugin.serialization") version "1.9.25"
    id("com.google.devtools.ksp") version "1.9.25-1.0.20"
}

if (hasGoogleServicesJson) {
    apply(plugin = "com.google.gms.google-services")
} else {
    logger.lifecycle("google-services.json not found. Building without Firebase resource generation.")
}


android {
    namespace = "com.starkaid.starkaidapp"
    compileSdk = 36

    defaultConfig {
        applicationId = "com.starkaid.starkaidapp"
        minSdk = 26
        targetSdk = 35
        versionCode = 63
        versionName = "6.3"
        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
        multiDexEnabled = true

        manifestPlaceholders += mutableMapOf(
            "redirectSchemeName" to "starkaid",
            "redirectHostName" to "callback",
            "admobAppId" to admobAppId,
            "unityAdsAppId" to unityAdsAppId
        )

        buildConfigField("boolean", "STARKAID_IS_DEVELOPMENT", starkAidIsDevelopment.toString())
        buildConfigField("String", "STARKAID_DEV_API_BASE_URL", "\"${escapeBuildConfig(starkAidDevApiBaseUrl)}\"")
        buildConfigField("String", "STARKAID_DEV_WEB_BASE_URL", "\"${escapeBuildConfig(starkAidDevWebBaseUrl)}\"")
        buildConfigField("String", "STARKAID_PROD_API_BASE_URL", "\"${escapeBuildConfig(starkAidProdApiBaseUrl)}\"")
        buildConfigField("String", "STARKAID_PROD_WEB_BASE_URL", "\"${escapeBuildConfig(starkAidProdWebBaseUrl)}\"")
        buildConfigField("String", "STARKAID_SPOTIFY_CLIENT_ID", "\"${escapeBuildConfig(starkAidSpotifyClientId)}\"")
        buildConfigField("String", "STARKAID_SPOTIFY_CLIENT_SECRET", "\"${escapeBuildConfig(starkAidSpotifyClientSecret)}\"")
        buildConfigField("String", "STARKAID_EWELINK_CLIENT_ID", "\"${escapeBuildConfig(starkAidEwelinkClientId)}\"")
        buildConfigField("String", "STARKAID_EWELINK_CLIENT_SECRET", "\"${escapeBuildConfig(starkAidEwelinkClientSecret)}\"")
    }

    buildTypes {
        release {
            isMinifyEnabled = true
            isShrinkResources = true
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }

    configurations.all {
        // Configuração removida - Tuya SDK não é mais usado
    }

    buildFeatures {
        buildConfig = true
        compose = true
        viewBinding = true
    }

    // Configurar sourceSets explicitamente para evitar problemas de "different root"
    sourceSets {
        getByName("main") {
            java.srcDirs("src/main/java")
            res.srcDirs("src/main/res")
            assets.srcDirs("src/main/assets", "../../starkaid-avatar")
            manifest.srcFile("src/main/AndroidManifest.xml")
        }
    }

    // Alinha Compose compiler com Kotlin 1.9.25
    @Suppress("UnstableApiUsage")
    composeOptions {
        kotlinCompilerExtensionVersion = "1.5.15"
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    @Suppress("DEPRECATION")
    kotlinOptions {
        jvmTarget = "17"
    }

    packaging {
        resources {
            excludes += listOf(
                "META-INF/DEPENDENCIES",
                "META-INF/INDEX.LIST",
                "META-INF/io.netty.versions.properties",
                "META-INF/FastDoubleParser-LICENSE",
                "META-INF/FastDoubleParser-NOTICE",
                "META-INF/thirdparty-LICENSE",
                "META-INF/thirdparty-NOTICE",
                "META-INF/*.kotlin_module",
                "META-INF/*.version",
                "META-INF/*.LICENSE*",
                "META-INF/*.NOTICE*",
                "META-INF/*.SF",
                "META-INF/*.DSA",
                "META-INF/*.RSA"
            )
        }
    }
}

// Task de pós-processamento para corrigir apenas strings com múltiplos '%' não posicionais
// Escopo autorizado: intermediários e src/main/res/values-*/strings.xml do app
// Regra: adiciona formatted="false" quando percentCount >= 2 e sem placeholders posicionais
// Comentário inserido junto à string alterada.
fun fixNonPositionalStrings(targetFile: File, intermediatesRoot: String, srcResRoot: String): Boolean {
    if (!targetFile.exists() || !targetFile.isFile) return false
    val path = targetFile.path
    val inIntermediates = path.contains(intermediatesRoot)
    val inSrcValues = path.startsWith(srcResRoot) && path.contains("${File.separator}values")
    if (!inIntermediates && !inSrcValues) return false

    val original = targetFile.readText()
    val pattern = Regex("<string\\s+name=\\\"([^\"]+)\\\"([^>]*)>(.*?)</string>", RegexOption.DOT_MATCHES_ALL)
    var changed = false
    val updated = pattern.replace(original) { match ->
        val name = match.groupValues[1]
        val attrs = match.groupValues[2]
        val body = match.groupValues[3]
        val hasFormatted = attrs.contains("formatted=")
        val hasPositional = body.contains("%1$") || body.contains("%2$") || body.contains("%3$")
        val percentCount = body.count { it == '%' }
        if (!hasFormatted && !hasPositional && percentCount >= 2) {
            changed = true
            """<!-- formatted=false aplicado para evitar erro de merge de resources (Gradle/AAPT) -->
<string name="$name"${attrs} formatted="false">$body</string>"""
        } else {
            match.value
        }
    }
    if (changed) {
        targetFile.writeText(updated)
    }
    return changed
}

// Hook antes do mergeResources: corrige intermediários e src/main/res/values-*/strings.xml
tasks.withType<com.android.build.gradle.tasks.MergeResources>().configureEach {
    doFirst {
        val intermediatesRoot = File(project.buildDir, "intermediates").absolutePath
        val srcResRoot = File(project.projectDir, "src${File.separator}main${File.separator}res").absolutePath
        var fixedCount = 0
        inputs.files.forEach { f ->
            if (f.isFile && f.extension == "xml") {
                if (fixNonPositionalStrings(f, intermediatesRoot, srcResRoot)) fixedCount++
            } else if (f.isDirectory) {
                f.walkTopDown().filter { it.isFile && it.extension == "xml" }.forEach { xml ->
                    if (fixNonPositionalStrings(xml, intermediatesRoot, srcResRoot)) fixedCount++
                }
            }
        }
        if (fixedCount > 0) {
            println("fixThirdPartyStrings: corrigidos $fixedCount arquivos (src/intermediários).")
        } else {
            println("fixThirdPartyStrings: nenhum ajuste necessário.")
        }
    }
}

configurations.all {
    resolutionStrategy.force(
        "org.jetbrains.kotlin:kotlin-stdlib:1.9.25",
        "org.jetbrains.kotlin:kotlin-stdlib-jdk7:1.9.25",
        "org.jetbrains.kotlin:kotlin-stdlib-jdk8:1.9.25",
        "org.jetbrains.kotlin:kotlin-stdlib-common:1.9.25"
    )
}

dependencies {
    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.appcompat)
    implementation(libs.material)
    implementation(libs.androidx.activity)
    implementation(libs.androidx.constraintlayout)
    implementation(libs.firebase.inappmessaging)
    implementation(libs.androidx.ui.tooling.preview)
    implementation(libs.androidx.media3.exoplayer)
    implementation(libs.transport.api)
    implementation(libs.androidx.swiperefreshlayout)
    testImplementation(libs.junit)
    androidTestImplementation(libs.androidx.junit)
    androidTestImplementation(libs.androidx.espresso.core)

    implementation("androidx.multidex:multidex:2.0.1")

    implementation(platform("com.google.firebase:firebase-bom:33.7.0"))
    implementation("com.google.firebase:firebase-analytics")
    implementation("com.google.firebase:firebase-messaging")

    implementation("com.squareup.retrofit2:retrofit:2.9.0")
    implementation("com.squareup.retrofit2:converter-gson:2.9.0")
    implementation("com.squareup.okhttp3:okhttp:4.12.0")
    implementation("com.squareup.okhttp3:logging-interceptor:4.12.0")
    implementation(platform("org.jetbrains.kotlin:kotlin-bom:1.9.25"))
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.7.3")
    implementation("com.auth0.android:jwtdecode:2.0.1")
    implementation("com.google.android.material:material:1.9.0")
    implementation("androidx.core:core-splashscreen:1.0.1")

    // ✅ Compose (alinhado ao Kotlin 1.9.25)
    implementation(platform("androidx.compose:compose-bom:2024.08.00"))
    implementation("androidx.compose.ui:ui")
    implementation("androidx.compose.ui:ui-graphics")
    implementation("androidx.compose.ui:ui-tooling-preview")
    implementation("androidx.compose.material3:material3")
    implementation("androidx.compose.runtime:runtime-livedata:1.9.0")
    implementation("androidx.lifecycle:lifecycle-viewmodel-compose:2.9.2")
    implementation("androidx.activity:activity-compose:1.10.1")
    implementation("androidx.navigation:navigation-compose:2.9.3")

    androidTestImplementation("androidx.compose.ui:ui-test-junit4:1.9.0")

    debugImplementation("androidx.compose.ui:ui-tooling")
    debugImplementation("androidx.compose.ui:ui-test-manifest")

    // ✅ Room Database
    val room_version = "2.7.2"
    implementation("androidx.room:room-runtime:$room_version")
    ksp("androidx.room:room-compiler:$room_version")
    implementation("androidx.room:room-ktx:$room_version")

    // ✅ AdMob e Google Play Services
    implementation("com.google.android.gms:play-services-ads:23.3.0")
    implementation("com.google.android.gms:play-services-base:18.5.0")
    implementation("com.google.android.gms:play-services-basement:18.4.0")

    implementation("com.google.ads.mediation:unity:4.11.3.0") {
        exclude(group = "com.unity3d.ads", module = "unity-ads")
        exclude(group = "com.google.ads.mediation.unity")
    }

    implementation("com.google.ads.mediation:unity:4.12.0.0")
    implementation("com.unity3d.ads:unity-ads:4.12.0")

    implementation("com.microsoft.signalr:signalr:8.0.0")

    // ✅ Lottie
    implementation("com.airbnb.android:lottie:6.1.0")

    implementation("androidx.core:core-ktx:1.12.0")
    implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.6.2")

    // ✅ SLF4J
    implementation("org.slf4j:slf4j-api:1.7.36")
    implementation("org.slf4j:slf4j-android:1.7.36")

    implementation("org.jetbrains.kotlinx:kotlinx-metadata-jvm:0.6.0")

    // ✅ ExoPlayer correto
    implementation("com.google.android.exoplayer:exoplayer-core:2.19.1")
    implementation("com.google.android.exoplayer:exoplayer-ui:2.19.1")

    // ✅ Spotify SDK
    implementation(fileTree("libs") { include("*.aar") })

    implementation("androidx.browser:browser:1.9.0")
    implementation("androidx.webkit:webkit:1.14.0")

    // ✅ AWS Transcribe
    implementation("software.amazon.awssdk:transcribestreaming:2.33.11")
    implementation("software.amazon.awssdk:auth:2.33.11")
    implementation("software.amazon.awssdk:regions:2.33.11")

    implementation("com.fasterxml.jackson.core:jackson-core:2.16.1")
    implementation("com.fasterxml.jackson.core:jackson-annotations:2.16.1")
    implementation("com.fasterxml.jackson.core:jackson-databind:2.16.1")

    implementation("com.google.api-client:google-api-client:2.2.0")
    // Cliente HTTP legado para APIs Google que dependem de javanet
    implementation("com.google.http-client:google-http-client:1.43.3")

// Última versão estável do Joda Time
    implementation("joda-time:joda-time:2.12.5")


    implementation(fileTree(mapOf("dir" to "libs", "include" to listOf("*.aar"))))
    implementation("com.alibaba:fastjson:1.1.67.android")
    implementation("com.squareup.okhttp3:okhttp-urlconnection:3.14.9")

    implementation("androidx.security:security-crypto:1.1.0-alpha06")
}


tasks.withType<org.jetbrains.kotlin.gradle.tasks.KotlinCompile>().configureEach {
    compilerOptions {
        jvmTarget.set(org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17)
        freeCompilerArgs.addAll(
            listOf(
                "-P",
                "plugin:androidx.compose.compiler.plugins.kotlin:reportsDestination=${layout.buildDirectory.asFile.get().absolutePath}/compose_metrics",
                "-P",
                "plugin:androidx.compose.compiler.plugins.kotlin:metricsDestination=${layout.buildDirectory.asFile.get().absolutePath}/compose_metrics"
            )
        )
    }
}
