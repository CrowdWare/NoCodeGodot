# SMS When Return
The SMS engine should make it possible that a when statement can return a value like in Kotlin.
Here is a sample:
```kotlin
var msg = when(lang) {
    "de" -> "Hallo Welt"
    "es" -> "¡Hola Mundo!"
    "fr" -> "Bonjour le monde !"
    "pt" -> "Olá Mundo!"
    else -> "Hello World"
}
```

## Unit Test
Please also add a unit test for this case.