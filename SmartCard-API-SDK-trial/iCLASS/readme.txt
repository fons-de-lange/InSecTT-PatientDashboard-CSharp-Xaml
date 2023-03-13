last review: 22NOV2019
desc: SmartCardAPI sample code for HID iCLASS access cards on HID OMNIKEY card readers

requirements:
Visual Studio 2013 or later
OMNIKEY smart card reader: 5x21, 5x22, 5x23, 5x27 
OMNIKEY sync API DLLs (for OK5x21 reades only)

This sample code reads raw PACS bit data from HID iCLASS and SEOS cards. 
It also shows how to extract facility and card numbers applying public card formats.

Note that there is a binary - PACSprobe - available online at https://pacsprobe.com
that also uses this SmartCardAPI card module as well as the HID Prox card module.

OMNIKEY 5x21: xchip readers require proprietary API DLLs:
- scardsyn.dll (for x86)
- scardsinx64.dll (for x64)
These are part of HID OMNIKEY synchronous card API available online www.hidglobal.com
and added to the local lib folder for your convenience. 

Please contact support@smartcard-api.com with any questions.


