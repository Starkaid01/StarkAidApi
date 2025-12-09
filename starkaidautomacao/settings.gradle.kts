pluginManagement {
    repositories {
        gradlePluginPortal()
        google()
        mavenCentral()
        maven { url = uri("https://jitpack.io") }
        maven { url = uri("https://developer.huawei.com/repo/") }
        maven { url = uri("https://maven-other.tuya.com/repository/maven-releases/") }
        maven { url = uri("https://maven-other.tuya.com/repository/maven-commercial-releases/") }
        flatDir {
            dirs("libs")
        }
    }
}

dependencyResolutionManagement {
    repositoriesMode.set(RepositoriesMode.FAIL_ON_PROJECT_REPOS)
    repositories {
        google()
        mavenCentral()
        maven { url = uri("https://jitpack.io") }
        maven { url = uri("https://developer.huawei.com/repo/") }
        maven { 
            url = uri("https://maven-other.tuya.com/repository/maven-releases/")
        }
        maven { 
            url = uri("https://maven-other.tuya.com/repository/maven-commercial-releases/")
        }
        flatDir {
            dirs("libs")
        }
    }
}

rootProject.name = "Starkaid Automation"
include(":app")