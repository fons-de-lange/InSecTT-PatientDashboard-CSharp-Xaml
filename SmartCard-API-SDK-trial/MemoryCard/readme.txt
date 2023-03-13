date: 10NOV2019
auth: MJ, marc@cardwerk.com

This sample code requires:

For OMNIKEY Contact Card Readers
================================
- OMNIKEY scardsyn.dll (32-bit) if built for x86 target; scardsynx64.dll (64 bit) for x64 target or AnyCPU on x64 system
  (the local solution copies scardsyn*.dll from local lib folder to targetdir)
  AND
  - OMNIKEY contact smart card reader with firmware version 2.03 or 2.04
 
  OR
  - OMNIKEY readers with dual interface and firmware version 5.10, 5.20, 5.31)

  OR
- OMNIKEY contact reader with Aviator chipset (no third-party API; all internal reader calls via pseudo APDU)


For Identiv (formerly SCM) Card Readers
=======================================
  MCSCM.DLL properly installed, together with CardWerk SmartCard.DLL


The following cards are supported:

I2C cards from ST-Microelectronics:
- ST14C02C, ST14C04C, ST14E32, M14C04, M14C16, M14C32, M14C64, M14128, M14256

I2C cards from GEMplus:
- GFM2K, GFM4K, GFM32K
           
I2C cards from Atmel:
- AT24C01A, AT24C02, AT24C04, AT24C08, AT24C16, AT24C164, AT24C32, AT24C64, AT24C128, AT24C256, 
  AT24CS128, AT24CS256, AT24C512, AT24C1024

2WBP cards from Infineon
- SLE4432, SLE4442

KNOWN ISSUES:
scardsynx64.dll: can't read error counter; API entry point missing in OMNIKEY proprietary API DLL for x64?!


