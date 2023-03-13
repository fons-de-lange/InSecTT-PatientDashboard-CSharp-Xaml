using System;
using System.Threading;
using Subsembly.SmartCard;
using System.Diagnostics;
using SmartCardAPI.CardModule.EMV;

// HIST:
// 17SEP2020 MJ improve minimum reader count
// 15OCT2019 MJ rename to EMVCard
// 27NOV2017 MJ initial version derived from HelloEMV

namespace SampleCode.EMVCard
{
    public class EMVCard
    {
        static int cardInsertions = 0;
        static string programInfo = String.Format("HelloEMV.Console rev.17SEP2020 \n{0}", SMARTCARDAPI.ApiVersionInfo);
        static bool ALWAYS_LOOK_FOR_MORE_CARD_READERS = false; // increases the number of readers we are looking for

        static int Main(string[] args)
        {
            Console.WriteLine(programInfo);

            // SmartCardAPI provides convenient events to track cards and readers. 
            CardTerminalManager.Singleton.CardInsertedEvent += new CardTerminalEventHandler(CardInsertedEvent);
            CardTerminalManager.Singleton.CardRemovedEvent += new CardTerminalEventHandler(CardRemovedEvent);
            CardTerminalManager.Singleton.CardTerminalLostEvent += new CardTerminalEventHandler(TerminalLostEvent);
            CardTerminalManager.Singleton.CardTerminalFoundEvent += new CardTerminalEventHandler(TerminalFoundEvent);

            try
            {
                int readerCountAtStartup = CardTerminalManager.Singleton.Startup(true); // true: auto register PC/SC readers
                DisplayNumberOfAvailableReaders();

                if (readerCountAtStartup > 0)
                {
                    string[] readerList = CardTerminalManager.Singleton.GetSlotNames();
                    int index = 0;
                    foreach (string reader in readerList)
                    {
                        Console.WriteLine(string.Format("Reader #{0}: {1}", index++, reader));
                    }
                    if (ALWAYS_LOOK_FOR_MORE_CARD_READERS)
                    {
                        // By default SlotCountMinimum is set to the number of readers detected at startup.
                        // We increase this value to continue looking for more readers.
                        CardTerminalManager.Singleton.SlotCountMinimum = readerCountAtStartup + 1;
                    }
                }
                else
                {
                    Console.WriteLine("ERROR: No card reader/terminal available on this system!");
                    Console.WriteLine("Please verify your PC/SC smart card system.\n" +
                                      "Is a smart card reader attached?\n" +
                                      "Is your PC/SC smart card service up and running?\n" +
                                      "Is the reader driver installed?\n");
                }
                while (!Console.KeyAvailable)
                {
                    Console.Write(".");
                    Thread.Sleep(500);
                }

            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
            }
            finally
            {

                if (CardTerminalManager.Singleton.StartedUp)
                {
                    CardTerminalManager.Singleton.Shutdown();
                }
                Console.WriteLine("END of HelloCard.Console");
            }
            return Environment.ExitCode != 0 ? Environment.ExitCode : 100;
        }

        /// <summary>
        /// This method is called when a card is inserted. We try to power it up and, show the ATR
        /// </summary>
        /// <param name="aSender"></param>
        /// <param name="aEventArgs"></param>	
        static void CardInsertedEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {
            CardTerminalSlot aCardSlot = aEventArgs.Slot;
            CardActivationResult nActivationResult;
            Console.Beep();
            Console.Clear();
            Console.WriteLine(programInfo);
            Console.WriteLine(String.Format("  Total readers: {0}", CardTerminalManager.Singleton.SlotCount));
            Console.WriteLine(String.Format("Card insertions: {0}", ++cardInsertions));


            // Acquire any processor card (T=0 or T=1) that may be present in the given card terminal slot
            string readerName = aCardSlot.CardTerminalName;
            Console.WriteLine("card inserted in " + readerName);
            HelloCardHelper.DisplayReaderProperties(aCardSlot);

            CardHandle aCard = aCardSlot.AcquireCard((CardTypes.T0 | CardTypes.T1), out nActivationResult); // power up card

            if (nActivationResult != CardActivationResult.Success)
            {
                Debug.Assert(aCard == null);

                switch (nActivationResult)
                {
                    case CardActivationResult.NoCard:
                        Console.WriteLine("Please insert card");
                        break;
                    case CardActivationResult.UnresponsiveCard:
                        Console.WriteLine("Unresponsive card.");
                        break;
                    case CardActivationResult.InUse:
                        Console.WriteLine("Card in use.");
                        break;
                    default:
                        Console.WriteLine("Can't power up card!");
                        break;
                }
                return;
            }
           

            // =========================== ATR DETECTION ======================================
            // Every card accessed through PC/SC must return an Answer To Reset (ATR). 
            // this ATR is available through a cardHandle object
            byte[] atr = aCard.GetATR();
            if (atr.Length == 0) throw new Exception("Invalid ATR");
            Console.WriteLine("ATR: " + CardHex.FromByteArray(atr, 0, atr.Length));
            try
            {
                Console.WriteLine("====== START of EMV card access test ======");
                aCardSlot.BeginTransaction();
                HelloCardHelper.AccessEmvCard(aCard);
            }
            catch (Exception x)
            {
                Trace.WriteLine(x.ToString());
                Console.WriteLine(x.ToString());
                Console.WriteLine("Card access error");
            }
            finally
            {
                aCardSlot.EndTransaction();
                aCard.Dispose(); // release card handle
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aSender"></param>
        /// <param name="aEventArgs"></param>	
        static void CardRemovedEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {
            CardTerminalSlot aCardSlot = aEventArgs.Slot;
            Console.Beep(1000, 200);
            Console.Beep(1000, 200);
            Console.Write("\ncard removed from " + aCardSlot.CardTerminalName);
            Console.Write("\nplease insert card");
        }


        /// <summary>
        /// This method is called whenever SmartCardAPI indicates a terminal that has been 
        /// added to the current tracking list.
        /// </summary>
        /// <param name="aSender"></param>
        /// <param name="aEventArgs"></param>
        static void TerminalFoundEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {

            if (CardTerminalManager.Singleton.StartedUp)
            {
                DisplayNumberOfAvailableReaders();
                Console.WriteLine("Found reader: " + aEventArgs.Slot.CardTerminalName);
                if (ALWAYS_LOOK_FOR_MORE_CARD_READERS && CardTerminalManager.Singleton.SlotCount == CardTerminalManager.Singleton.SlotCountMinimum)
                {
                    CardTerminalManager.Singleton.SlotCountMinimum += 1; // let's continue looking for more readers
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aSender"></param>
        /// <param name="aEventArgs"></param>	
        static void TerminalLostEvent(object aSender, CardTerminalEventArgs aEventArgs)
        {
            if (CardTerminalManager.Singleton.StartedUp)
            {
                Console.WriteLine("\nlost reader " + aEventArgs.Slot.CardTerminalName);
                if (CardTerminalManager.Singleton.SlotCount == 0)
                {
                    Console.WriteLine("Please connect reader so we can start accessing smart cards");
                    // start looking for reader insertion
                    // done automatically by the singleton. The singleton raises a "found reader" event if it finds a new reader.
                }
                else if (CardTerminalManager.Singleton.SlotCountMinimum + 1 > CardTerminalManager.Singleton.SlotCount)
                {
                    CardTerminalManager.Singleton.SlotCountMinimum -= 1;
                }
            }
        }





        /// <summary>
        /// Displays number of readers currently available. Unless overwritted by the host application,
        /// the requested number of readers is at least one or the number of readers connected at program 
        /// start. Whichever is greater.
        /// </summary>
        static void DisplayNumberOfAvailableReaders()
        {
            int slotCount = CardTerminalManager.Singleton.SlotCount;
            int slotCountMinimum = CardTerminalManager.Singleton.SlotCountMinimum;
            Console.WriteLine("Available readers: " + slotCount);
            if (slotCount < slotCountMinimum)
            {
                Console.WriteLine(String.Format("Looking for {0} more reader(s)", slotCountMinimum - slotCount));
            }
        }


    } //program


    public static class HelloCardHelper
    {

        /// <summary>
        /// We display reader properties available via CardTerminal class. Note that availability
        /// of these properties depends on the terminal.
        /// </summary>
        /// <param name="aCardSlot">current slot, activated and with powered card</param>
        public static void DisplayReaderProperties(CardTerminalSlot aCardSlot)
        {
            string defaultInfo = "not available";
            // we now have a reader and a powered card

            Console.WriteLine("*** Card terminal info ***");

            string manufacturerName = aCardSlot.CardTerminal.ManufacturerName;
            if (manufacturerName == null) manufacturerName = defaultInfo;
            Console.WriteLine("manufacturer: " + manufacturerName);

            string productName = aCardSlot.CardTerminal.ProductName;
            if (productName == null) productName = defaultInfo;
            Console.WriteLine("name:      " + productName);

            string productVersion = aCardSlot.CardTerminal.ProductVersion;
            if (productVersion == null) productVersion = defaultInfo;
            Console.WriteLine("version: " + productVersion);

            string productSerialNumber = aCardSlot.CardTerminal.ProductSerialNumber;
            if (productSerialNumber == null) productSerialNumber = defaultInfo;
            Console.WriteLine("serial number: " + productSerialNumber);

            string productAdditionalInfo = aCardSlot.CardTerminal.ProductAdditionalInfo;
            if (productAdditionalInfo == null) productAdditionalInfo = defaultInfo;
            Console.WriteLine("additional info:  " + productAdditionalInfo);

            string physicalConnection = aCardSlot.CardTerminal.PhysicalConnection;
            if (physicalConnection == null) physicalConnection = defaultInfo;
            Console.WriteLine("physical connection:  " + physicalConnection);

            Console.WriteLine("*** End of card terminal info ***");

        }
        /// <summary>
        ///     CardModule.EMV takes care of everything necessary to explore the card and retrieves
        ///     all static data. Such data is available after instantiating an EmvCard. Use the 
        ///     EmvCardData object to access static card data.
        /// </summary>
        /// <remarks>
        ///     Our EMV card modul is work in progress. It is really meant to give you easy access to our
        ///     sample code and SmartCardAPI with a card millions of people already have in their wallet.
        ///
        ///     This is by no means a full implementation of an EMV card library for financial applications.
        ///     We'll be happy to do this as consulting and software development work :-)
        ///     Note that the CardHandle represents the combination of card terminal and
        ///     powered-up card. It can be used to access the card outside the scope of the EmvCard object.
        /// </remarks>
        /// <param name="aCard">SmartCardAPI card hendle object</param>

        public static void AccessEmvCard(CardHandle aCard)
        {
            
                EmvCard emvCard = new EmvCard(aCard);
                if (emvCard == null) throw new Exception("ERROR: can't instantiate EMVcard object");
                Console.WriteLine("CardInfo: " + emvCard.CardName);
                if (!emvCard.IsEMV) throw new Exception("ERROR: not an EMV card");
                EmvCardData cardData = emvCard.CardData; // contains all EMV data we can read off the card
                if (cardData != null)
                {
                    Console.WriteLine(cardData.DataLog);
                    if (cardData.IsValid())
                    {
                        Console.WriteLine("===All mandatory data objects available===");
                        Console.WriteLine("Expiration date:        " + CardHex.FromByteArray(cardData.ApplicationExpirationDate));
                        Console.WriteLine("Primary Account Number: " + CardHex.FromByteArray(cardData.ApplicationPrimaryAccountNumber));
                        Console.WriteLine("CDOL1:                  " + CardHex.FromByteArray(cardData.CardRiskManagementDataObjectList1));
                        Console.WriteLine("CDOL2:                  " + CardHex.FromByteArray(cardData.CardRiskManagementDataObjectList2));

                        byte[] transactionCounter = emvCard.GetData(EMV_TAG.ApplicationTransactionCounter);
                        if (transactionCounter != null) Console.WriteLine("Application Transaction Counter: " + CardHex.FromByteArray(transactionCounter));

                        byte[] LastOnlineApplicationTransactionCounter = emvCard.GetData(EMV_TAG.LastOnlineApplicationTransactionCounterRegister);
                        if (LastOnlineApplicationTransactionCounter != null) Console.WriteLine("Last Online Application Transaction Counter: " + CardHex.FromByteArray(LastOnlineApplicationTransactionCounter));

                        // the following data items are not always available. don't be surprised if they are null

                        // List (in tag and length format) of data objects representing the logged data elements that are passed to the terminal when a transaction log record is read
                        byte[] LogFormat = emvCard.GetData(EMV_TAG.LogFormat);
                        if (LogFormat != null) Console.WriteLine("LogFormat: " + CardHex.FromByteArray(LogFormat));

                        // PIN retry counter
                        byte[] PersonalIdentificationNumberTryCounter = emvCard.GetData(EMV_TAG.PersonalIdentificationNumberTryCounter);
                        if (PersonalIdentificationNumberTryCounter != null) Console.WriteLine("Remaining PIN tries: " + CardHex.FromByteArray(PersonalIdentificationNumberTryCounter));

                        Console.WriteLine("===Optional data objects===");
                        if (cardData.CardholderVerificationMethodList != null)
                        {
                            // CVM list - we are exploring this list here

                            Console.WriteLine("CVM List:               " + CardHex.FromByteArray(cardData.CardholderVerificationMethodList));
                            //NOTE: A CVM List with no Cardholder Verification Rules is considered to be the same as a CVM List not being present.
                            byte[] cvmList = cardData.CardholderVerificationMethodList;
                            if (cvmList.Length % 2 != 0)
                            {
                                Console.WriteLine("ERROR: odd number of bytes (that is, with an incomplete CVM Rule), \n the terminal shall terminate the transaction as specified in Book 3 section 7.5.");
                            }
                            if (cvmList.Length < 4 + 4)
                            {
                                Console.WriteLine("ERROR: CVM list too short; unexpected length!");
                            }
                            // An amount field (4 bytes, binary format), referred to as ‗X‘ in Table 40: CVM Condition Codes. 
                            // 'X' is expressed in the Application Currency Code with implicit decimal point. For example, 123 
                            // (hexadecimal '7B') represents £1.23 when the currency code is '826'.
                            Console.WriteLine("      amount  field: " + CardHex.FromByteArray(cvmList, 0, 4));

                            // A second amount field (4 bytes, binary format), referred to as 'Y' in Table 40. 
                            // 'Y' is expressed in Application Currency Code with implicit decimal point. For example, 123 (hexadecimal '7B') 
                            // represents £1.23 when the currency code is '826'.
                            Console.WriteLine("second amount field: " + CardHex.FromByteArray(cvmList, 4, 4));

                            if (cvmList.Length == 8)
                            {
                                Console.WriteLine("WARNING: No Cardholder Verification Rules. This is considered to be the same as a CVM List not being present.");
                                Console.WriteLine("         Therefore terminal must not set 'Cardholder verification was performed' bit in the TSI.");
                            }
                            else
                            {

                                // A variable-length list of two-byte data elements called Cardholder Verification Rules (CV Rules). Each CV Rule 
                                // describes a CVM and the conditions under which that CVM should be applied (see Annex C3).
                                int numberOfCardholderVerificationRules = (cvmList.Length - 8) / 2;
                                Console.WriteLine(" number of CV rules: " + numberOfCardholderVerificationRules.ToString());
                                bool OnErrorApplyNextRule = true;
                                bool terminalSupportsCvmRule = false;
                                for (int r = 0; r < numberOfCardholderVerificationRules && OnErrorApplyNextRule == true && terminalSupportsCvmRule == false; r++)
                                {
                                    Console.WriteLine("CVM rule #" + (1 + r).ToString());
                                    byte cvmCode = cvmList[8 + 2 * r];
                                    byte cvmCondition = cvmList[8 + 2 * r + 1];
                                    OnErrorApplyNextRule = (byte)(cvmCode & (byte)CVMCODE.OnErrorApplyNextRule) == (byte)CVMCODE.OnErrorApplyNextRule ? true : false;

                                    // this is something theTerminal must do
                                    string terminalMessage = "";
                                    byte strippedCvmCode = (byte)(0x3F & cvmCode);  // strip b8, b7
                                    switch (strippedCvmCode)
                                    {

                                        case (byte)CVMCODE.PlaintextPinIcc:
                                            terminalMessage += "Plaintext PIN verification performed by ICC.";
                                            break;

                                        case (byte)CVMCODE.EncypheredPinOnline:
                                            terminalMessage += "Enciphered PIN verified online.";
                                            break;

                                        case (byte)CVMCODE.PlaintextPinAndPaperSignature:
                                            terminalMessage += "Plaintext PIN verification performed by ICC and signature (paper).";
                                            break;

                                        case (byte)CVMCODE.EncypheredPinIcc:
                                            terminalMessage += "Enciphered PIN verification performed by ICC.";
                                            break;

                                        case (byte)CVMCODE.EncypheredPinAndPaperSignature:
                                            terminalMessage += "Enciphered PIN verification performed by ICC and signature (paper).";
                                            break;

                                        case (byte)CVMCODE.PaperSignature:
                                            terminalMessage += "Paper signature.";
                                            terminalSupportsCvmRule = true;
                                            break;

                                        case (byte)CVMCODE.NoCvmRequired:
                                            terminalMessage += "No CVM required.";
                                            terminalSupportsCvmRule = true;
                                            break;

                                        default:
                                            terminalMessage += "CVM not supported by terminal";
                                            break;

                                    }


                                    Debug.WriteLine("CVM condition for this rule: " + CardHex.FromByte(cvmCondition));
                                    terminalMessage += " condition: ";
                                    switch (cvmCondition)
                                    {
                                        case (byte)CVMCONDITION.Always:
                                            terminalMessage += "Always.";
                                            break;

                                        case (byte)CVMCONDITION.IfUnattendedCash:
                                            terminalMessage += "If unattended cash.";
                                            break;

                                        case (byte)CVMCONDITION.IfNotUnattendedCash:
                                            terminalMessage += "If not unattended cash and not manual cash and not purchase with cashback.";
                                            break;

                                        case (byte)CVMCONDITION.IfTerminalSupportsCvm:
                                            terminalMessage += "If terminal supports the CVM.";
                                            break;

                                        case (byte)CVMCONDITION.IfManualCash:
                                            terminalMessage += "If manual cash.";
                                            break;

                                        case (byte)CVMCONDITION.IfPurchaseWithCashback:
                                            terminalMessage += "If purchase with cashback.";
                                            break;

                                        case (byte)CVMCONDITION.IfTransactionUnderXval:
                                            terminalMessage += "If transaction is in the application currency and is under X value.";
                                            break;

                                        case (byte)CVMCONDITION.IfTransactionOverXval:
                                            terminalMessage += "If transaction is in the application currency and is over X value.";
                                            break;

                                        case (byte)CVMCONDITION.IfTransactionUnderYval:
                                            terminalMessage += "If transaction is in the application currency and is under Y value.";
                                            break;

                                        case (byte)CVMCONDITION.IfTransactionOverYval:
                                            terminalMessage += "If transaction is in the application currency and is over Y value.";
                                            break;

                                        default:
                                            terminalMessage += "WARNING: unknown CVM condition.";
                                            break;

                                    }

                                    Console.WriteLine(terminalMessage);
                                    if (!terminalSupportsCvmRule)
                                    {
                                        Console.WriteLine("WARNING: terminal does not support this CVM rule");
                                    }
                                    else
                                    {
                                        Console.WriteLine("OK - terminal supports this CVM rule.");
                                        // unless CVM rule is "No CVM required", we need to branch out to paper signature terminal software module
                                    }

                                    if (!terminalSupportsCvmRule)
                                    {

                                        if (OnErrorApplyNextRule && r + 1 < numberOfCardholderVerificationRules)
                                        {
                                            Console.WriteLine("Apply next rule.");
                                        }
                                        else
                                        {
                                            Console.WriteLine("ERROR: no other rules accepted by the card. Terminal can't satisfy CV rules.");
                                        }
                                    }
                                    else
                                    {
                                        // terminal does CVM
                                        // When cardholder verification is completed, the terminal shall 
                                        //  - set the CVM Results according to Book 4 section 6.3.4.5 
                                        //  - set the "Cardholder verification was performed" bit in the TSI to 1
                                        if (strippedCvmCode == (byte)CVMCODE.PaperSignature)
                                        {
                                            Console.WriteLine("TODO: EMV TERMINAL CHECKS PAPER SIGNATURE");
                                            /*
                                             * from EMVco book 4, v4.3 
                                             * 6.3.4.4 Signature (Paper)
                                             * When the applicable CVM is signature, the terminal shall set byte 3 of the CVM Results 
                                             * to ‘unknown’. At the end of the transaction, the terminal shall print a receipt with a 
                                             * line for cardholder signature. (See Annex A2 for requirements for the terminal to support 
                                             * signature as a CVM.)
                                             * also see "A4 CVM Results" of book 4, v4.3
                                             */
                                            byte[] cvmResults = { 0, 0, 0 };
                                            cvmResults[0] = cvmCode;      // last CVM code
                                            cvmResults[1] = cvmCondition; // last CVM condition
                                            cvmResults[2] = 0;            // unknown ... signature will be checked after the card transaction 
                                            //Console.WriteLine("AuthorizeNet->CVM results: " + CardHex.FromByteArray(cvmResults)); // we need to set 
                                        }
                                    }



                                } //for all rules

                            }
                        }
                        else
                        {
                            Console.WriteLine("CVM List empty: terminal must not set 'Cardholder verification was performed' bit in the TSI.");
                        }

                    } // EMV card data
                    else
                    {
                        Console.WriteLine("ERROR: Invalid EMV card data.");
                    }

                }

                Console.WriteLine("====== END of EMV card access test ======");

            } //EMV test
        } // helper class
    }
