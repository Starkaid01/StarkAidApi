import org.gradle.kotlin.dsl.implementation

// Top-level variable for Room version
val room_version = "2.7.2"

plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.android)
    alias(libs.plugins.google.services)
    alias(libs.plugins.compose.compiler)
    id("org.jetbrains.kotlin.plugin.serialization") version "1.9.20"
    id("org.jetbrains.kotlin.kapt")
}


android {
    namespace = "com.starkaid.starkaidapp"
    compileSdk = 36

    defaultConfig {
        applicationId = "com.starkaid.starkaidapp"
        minSdk = 26
        targetSdk = 35
        versionCode = 57
        versionName = "5.7"
        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
        multiDexEnabled = true

        manifestPlaceholders += mutableMapOf(
            "redirectSchemeName" to "starkaid",
            "redirectHostName"   to "callback"
        )
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
            assets.srcDirs("src/main/assets")
            manifest.srcFile("src/main/AndroidManifest.xml")
        }
    }

    @Suppress("UnstableApiUsage")
    composeOptions {
        kotlinCompilerExtensionVersion = "1.5.14"
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
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.7.3")
    implementation("com.auth0.android:jwtdecode:2.0.1")
    implementation("com.google.android.material:material:1.9.0")
    implementation("androidx.core:core-splashscreen:1.0.1")

    // ✅ Compose oficial (corrigido)
    implementation(platform("androidx.compose:compose-bom:2025.08.00"))
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
    kapt("androidx.room:room-compiler:$room_version")
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

    implementation("com.microsoft.signalr:signalr:7.0.5")

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