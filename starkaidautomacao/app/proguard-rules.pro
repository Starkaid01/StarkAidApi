#############################################
# ============== REGRAS GERAIS ==============
#############################################

# Mantém classes nativas
-keepclasseswithmembernames class * {
    native <methods>;
}

# Mantém métodos do View
-keepclassmembers public class * extends android.view.View {
    void set*(***);
    *** get*();
}

# Mantém classes de parcelamento
-keep class * implements android.os.Parcelable {
    public static final android.os.Parcelable$Creator *;
}

# Kotlin
-keep class kotlin.** { *; }
-dontwarn kotlin.**

#############################################
# ========== RETROFIT + GSON ================
#############################################

-keepattributes Signature, Exceptions, InnerClasses
-keepattributes *Annotation*
-keepclassmembers class * {
    @com.google.gson.annotations.SerializedName <fields>;
}
-keepclassmembers class com.starkaid.starkaidapp.models.** {
    public <init>(...);
}
-keep class retrofit2.** { *; }
-keepclasseswithmembers class * {
    @retrofit2.http.* <methods>;
}

#############################################
# ======= MODELOS/VIEWMODELS DO APP =========
#############################################

-keep class com.starkaid.starkaidapp.models.** { *; }
-keep class com.starkaid.starkaidapp.services.** { *; }
-keep class com.starkaid.starkaidapp.adapters.** { *; }
-keep class com.starkaid.starkaidapp.data.** { *; }
-keep class com.starkaid.starkaidapp.screens.** { *; }
-keep class com.starkaid.starkaidapp.ui.** { *; }
-keep class com.starkaid.starkaidapp.util.** { *; }
-keep class com.starkaid.starkaidapp.viewmodels.** { *; }

-keep class com.starkaid.starkaidapp.models.UserRegisterRequest { *; }

#############################################
# =============== UNITY ADS =================
#############################################

-keep class com.unity3d.ads.** { *; }
-keep interface com.unity3d.ads.** { *; }
-keep class * implements com.unity3d.ads.IUnityAdsInitializationListener { *; }
-keep class * implements com.unity3d.ads.IUnityAdsLoadListener { *; }
-keep class * implements com.unity3d.ads.IUnityAdsShowListener { *; }
-keep class * implements com.unity3d.ads.IUnityAdsTokenListener { *; }

-dontwarn com.unity3d.ads.**
-dontwarn com.unity3d.services.**
-dontwarn com.unity3d.player.**

#############################################
# ============== ADICIONAIS =================
#############################################

-keep class com.auth0.android.jwt.** { *; }
-keep class java.lang.Void { *; }

-keepclassmembers class * {
    public static * bind(android.view.View);
    public static * inflate(android.view.LayoutInflater);
}

-keepclasseswithmembers class * {
    public <init>(android.content.Context, android.util.AttributeSet);
    public <init>(android.content.Context, android.util.AttributeSet, int);
}

-dontwarn org.slf4j.**
-keep class org.slf4j.impl.StaticLoggerBinder { *; }

#############################################
# =============== SIGNALR ===================
#############################################

-keep class com.microsoft.signalr.** { *; }
-keep class com.microsoft.signalr.transport.** { *; }
-keep class com.microsoft.signalr.hubprotocol.** { *; }
-keep class okhttp3.** { *; }
-dontwarn okhttp3.**
-keep class com.google.gson.** { *; }
-dontwarn com.google.gson.**
-keep class com.microsoft.signalr.HubMessage { *; }
-keep class com.microsoft.signalr.Handshake* { *; }
-keep class com.microsoft.signalr.InvocationMessage { *; }
-keep class com.microsoft.signalr.CompletionMessage { *; }
-keep class com.microsoft.signalr.CancelInvocationMessage { *; }
-keep class com.microsoft.signalr.CloseMessage { *; }
-keep class com.microsoft.signalr.JsonHubProtocol { *; }
-keep class com.microsoft.signalr.HubProtocol* { *; }

#############################################
# ===== GOOGLE PLAY SERVICES + FIREBASE =====
#############################################

-keep class com.google.android.gms.** { *; }
-dontwarn com.google.android.gms.**
-keep class com.google.firebase.** { *; }
-dontwarn com.google.firebase.**

#############################################
# =========== SPOTIFY SDK ==================
#############################################

-keep class com.spotify.android.appremote.** { *; }
-keep class com.spotify.protocol.** { *; }
-dontwarn com.spotify.protocol.**
-keep class com.fasterxml.jackson.** { *; }
-dontwarn com.fasterxml.jackson.**

#############################################
# =========== AWS SDK ======================
#############################################

-keep class software.amazon.awssdk.** { *; }
-dontwarn software.amazon.awssdk.**
-keepattributes Signature, *Annotation*

#############################################
# =========== NETTY / BLOCKHOUND ============
#############################################

-dontwarn io.netty.util.internal.**
-dontwarn reactor.blockhound.**
-keep class io.netty.** { *; }
-keepclassmembers class io.netty.** { *; }

#############################################
# =========== SUPPRESS AUTOMÁTICO ===========
#############################################

-dontwarn com.aayushatharva.brotli4j.**
-dontwarn com.github.luben.zstd.**
-dontwarn com.google.protobuf.**
-dontwarn com.jcraft.jzlib.**
-dontwarn com.ning.compress.lzf.**
-dontwarn com.ning.compress.BufferRecycler
-dontwarn com.oracle.svm.core.annotate.**
-dontwarn com.spotify.base.annotations.NotNull
-dontwarn io.netty.internal.tcnative.**
-dontwarn lzma.sdk.**
-dontwarn net.jpountz.lz4.**
-dontwarn net.jpountz.xxhash.**
-dontwarn org.eclipse.jetty.alpn.**
-dontwarn org.eclipse.jetty.npn.**
-dontwarn org.ietf.jgss.**
-dontwarn org.jboss.marshalling.**
-dontwarn sun.security.x509.**

# ============== REGRAS TUYA ESPECÍFICAS ==============
-keep class com.thingclips.** { *; }
-keep class com.tuya.** { *; }
-dontwarn com.thingclips.**
-dontwarn com.tuya.**

# Classes específicas do Matter que estão gerando warnings
-keep class com.thingclips.sdk.matterlib.bdqqqpq { *; }
-keep class com.thingclips.sdk.matterlib.qbqppdq { *; }
-keep class com.thingclips.sdk.matterlib.pdbpddd { *; }
-keep class com.thingclips.sdk.matterlib.dbqqppp { *; }
-keep class com.thingclips.sdk.matterlib.qbqppdb { *; }
-keep class com.thingclips.sdk.matterlib.qpqbbpp { *; }

# Facebook SoLoader
-keep class com.facebook.soloader.** { *; }
-dontwarn com.facebook.soloader.**

# APIs Tuya
-keep class com.thingclips.smart.api.** { *; }
-dontwarn com.thingclips.smart.api.**
-keep class com.thingclips.smart.api.service.** { *; }
-dontwarn com.thingclips.smart.api.service.**

# Audio Engine
-keep class com.thingclips.smart.audioengine.** { *; }
-dontwarn com.thingclips.smart.audioengine.**

# Utilitários Tuya
-keep class com.thingclips.smart.base.utils.** { *; }
-dontwarn com.thingclips.smart.base.utils.**
-keep class com.thingclips.smart.utils.** { *; }
-dontwarn com.thingclips.smart.utils.**

# Cloud Storage
-keep class com.thingclips.smart.cloudstorage.** { *; }
-dontwarn com.thingclips.smart.cloudstorage.**

# Stat API
-keep class com.thingclips.smart.statapi.** { *; }
-dontwarn com.thingclips.smart.statapi.**

# Apache Commons
-keep class org.apache.** { *; }
-dontwarn org.apache.**
-keep class javax.servlet.** { *; }
-dontwarn javax.servlet.**

# Manter todos os companions
-keepclassmembers class ** {
    public static ** Companion;
}

# Manter métodos nativos
-keepclasseswithmembernames class * {
    native <methods>;
}


-keep class com.google.api.client.** { *; }
-dontwarn com.google.api.client.**

-keep class com.google.httpclient.** { *; }
-dontwarn com.google.httpclient.**

-keep class org.joda.time.** { *; }
-dontwarn org.joda.time.**

# Ignorar o KeysDownloader do Tink (não é usado em Android)
-dontwarn com.google.crypto.tink.util.KeysDownloader


# Google API Client (suprimir warnings de HTTP transport)
-dontwarn com.google.api.client.http.GenericUrl
-dontwarn com.google.api.client.http.HttpHeaders
-dontwarn com.google.api.client.http.HttpRequest
-dontwarn com.google.api.client.http.HttpRequestFactory
-dontwarn com.google.api.client.http.HttpResponse
-dontwarn com.google.api.client.http.HttpTransport
-dontwarn com.google.api.client.http.javanet.NetHttpTransport$Builder
-dontwarn com.google.api.client.http.javanet.NetHttpTransport

# Manter classes utilizadas internamente pelo Google API Client
-keep class com.google.api.client.** { *; }
-dontwarn com.google.api.client.**

# Joda-Time
-keep class org.joda.time.** { *; }
-dontwarn org.joda.time.**