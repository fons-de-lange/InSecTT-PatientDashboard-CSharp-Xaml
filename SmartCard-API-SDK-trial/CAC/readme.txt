date: 29OCT2019
auth: MJ

This sample code shows how to detect a Commmon Access Card (CAC) using CardWerk SmartCard API.
Please refer to dedicated PIV card sample code for NIST PIV, TSA TWIC and CIV cards 

- detects DoD CAC
- reads FCI 
- asks for PIN via SmartCard CardDialog
- supports secure PIN entry readers (via CardDialog)
- displays printed information in txt box
- displays personnel instance data

It is work in progress. Upon demand we are happy to extend the functionality.
Please contact us at support@smartcard-api.com

HISTORY:

29OCT2019
- improve lost/found reader handling
- move code to neqw repo
- build against .NET framework 4.5
- fix: PIN dialog shows if PIV card is presented instead of CAC card

11APR2017
- extracted CAC related code from PIV/CAC combo sample code 

17OCT2016
- fix: transaction protection not closed for CAC card, causing lost terminal error
- CAC card personnel data support

15JUL2014
- cert with < 10 byte length will be ignored
- invalid auth cert will be ignored (we display warning)

06FEB2013
- X509 certificate support
- card capability support
 
14MAY2012 MJ
 - introduce dedicated card modules: CardModule.PIV.DLL, CardModule.CAC.DLL
 - detect DoD CAC, TSA TWIC
 - CAC: read Person Instance
 - support for GSCIS/PIV transitional applet
 - Select card applet by AID to allow TWIC applet selection

07MAY2012 MJ 
 - support for CCC (selected via GSC IS AID) 