SmartCard API (Professional) SDK
--------------------------------

release: 17JAN2021 (latest release is available at https://smartcard-api.com)

REQUIREMENTS
============
Visual Studio 2017 or greater
for sample code: Microsoft .NET Framework version 4.5
PC/SC compliant card reader

IMPORTANT NOTE
==============
The SmartCard API (Professional) is copyrighted by CardWerk Technologies, Kennesaw, GA, USA. 
The SmartCard API (Professional) is licensed according to the licence conditions found in CardWerk.SmartCardApi.License.en.pdf

Folder------Description 
==============================================================================================================================
doc         documentation
lib         libraries for .NET frameworks: 3.5, 4.5. 4.6.1

HelloCard   sample code for any card supported by your card reader; includes console, GUI, C#, VB variants
            -> detects ATR
            -> shows usage of card inserted/removed events
            -> shows usage of reader connected/disconnected events

CAC         sample code for U.S. Common Access Card, reads Person and Personnel Instance data. 
DESFire     sample code for NXP DESfire EV1 and EV2 in EV1 mode
eGK         sample code for German Health Insurance cards. Supports KVK, eGK G1, eGK G2. C# and VB.NET projects are available.
EMVcard     sample code for Euro-Visa-Master credit & debit cards
            -> accessing static card data
            -> not meant for financial transactions. This would require additional cryptographic software, hardware modules and EMV kernel approval
            -> chances are that you can run a few basic tests with SmartCardAPI using the card you already have in your wallet 
Geldkarte   sample code for German Geldkarte

HID PACS cards:
sample code for HID physical access control systems (PACS) cards
PROXcard    -> sample code for HID PROX card access
iCLASS      -> sample code for HID iCLASS card access on OMNIKEY 5x21, 5x22, 5023 and 5x27 readers
SEOS        -> sample code for HID SEOS card access on OMNIKEY 5023, and 5x27 readers

iCODE       sample code for NXP iCODE cards (ISO/IEC 15693)
MemoryCard  sample code for synchronous, contact-based storage cards on OMNIKEY Xchip and Aviator readers
            -> requires card reader that supports synchtronousw cards
            -> this sample supports many chip types including SLE4442, 
Mifare      sample code for NXP Mifare Classic cards. Requires a PC/SC 2.01 part 3 compliant card reader.  
NFCtag      sample code for NFC tags based on Mifare Ultralight and NTAG chips. Demonstrates read/write access to such chips. 
            -> raw access to card data; does not require any NDEF data 
PIVcard     sample code for U.S. Gov issued PIV, TWIC or corporate CIV card.
            -> demonstrates access of CHUID, FASCN and printed card information 
            -> read X509 certificates and use private key for offline authentication and signing processes


HOW TO GET STARTED
==================
Try the HelloCard sample application first - it demonstrates card tracking, detects an ATR and should work with 
any contact or contactless card. A console version "HelloCard" with similar functionality removes all GUI elements making it
even easier to become familiar with SmartCardAPI

For VB.NET programmers HelloCard.VB is a great starting point

This release of the CardWerk SmartCard API (Professional) Development Kit contains the actual product, the product documentation 
and fully commented C# sample applications. Please refer to SmartCardApi.DevGuide.pdf for detailed documentation of
CardWerk SmartCard API (Professional). This development guide provides a product and architecture overview.

Please note that CT-API is not used in the USA. It is a legacy API that will most likely disappear in the near future. It is a great choice for 
legacy cards such as SLE 4442 because reader manufacturers support this type of synchronous card via CT-API.


HOW TO GET HELP / MORE FEATURES
===============================
The latest version of this SDK is available at https://smartcard-api.com
For technical support, contact us at support@smartcard-api.com
For feature requests and custom software development, contact Marc@cardwerk.com

Project examples:
- develop card personalization software/plugin
- develop JavaCard or MULTOS application
- integrate proprietary cards or readers
- interface with smart card system with host software written Java, Python, Qt, C#, C/C++
- printer encoder plugin software development
- smart card software development and consulting services
