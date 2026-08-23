# Rimworld True Child Aging

TODO: Fill in this README. See `PLAN.md` for the intended design.

Intended feature set (from PLAN.md):

- An integer **Child Aging Factor** slider from -5x to +5x, defaulting to RimWorld's +4x.
- Positive values replace the normal child-aging factor.
- 0x freezes biological aging while the pawn remains in RimWorld's child-aging range.
- Negative values actively correct biological age toward chronological age, but never below it
  (requires a Harmony extension to `Pawn_AgeTracker`, since vanilla cannot process negative
  biological-tick accumulation).

## Building

Requires [.NET SDK](https://dotnet.microsoft.com/) and the RimWorld game assemblies under
`/rimworld/<version>/`. The build copies the mod into `/rimworld/<version>/Mods/TrueChildAging`.

```bash
./build.sh
```

## License

**CC-BY-ND-4.0**
Creative Commons NoDerivations v4.0: Please see the [license file](LICENSE.md) for more information.

**YOU MAY FORK THIS PROJECT.**

**YOU MAY NOT PUBLISH ANY DERIVATION of this project to either your own website or a third-party host.**

**YOU MAY NOT PUBLISH ANY DERIVATION ON STEAM WORKSHOP WITHOUT EXPLICIT APPROVAL.**

## Contributors

[Theodore R. Smith](https://github.com/hopeseekr/) <hopeseekr@gmail.com>  
GPG Fingerprint: D8EA 6E4D 5952 159D 7759  2BB4 EEB6 CE72 F441 EC41  
WhatsApp / Signal: +1 832-303-9477
