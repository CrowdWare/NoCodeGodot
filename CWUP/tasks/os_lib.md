# OS Lib
We need an additional library named "os" that can be called from SMS.
And the first functions we will implement are:
```
os.getLocale()      // "de_DE"
os.getLanguage()    // "de"
os.getCountry()     // "DE"
os.getTimeZone()    // "Europe/Berlin"

os.getPlatform()    // "mac", "linux", "windows", "android"
os.getArch()        // "arm64", "x64"
os.isMobile()       // true|false
os.isDesktop()      // true|false

os.now()            // epoch ms
os.getUptime()      // seconds since app start
```

## Documentation
Please add the docs in /tools/specs