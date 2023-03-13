// --------------------------------------------------------------------------------------------
// HelloCardHelper.cs
// Helper methods for HelloCard sample code.
// This is part of CardWerk's SmartCardAPI (professional)
// Copyright © 2017-2019 CardWerk Technologies
// -------------------------------------------------------------------------------------------- 
// 02JUN2017 MJ initial version
// ============================= www.cardwerk.com =============================================

using System;
using System.Threading;
using System.Diagnostics;
using Subsembly.SmartCard;
using SmartCardAPI.CardModule.CLICS;

namespace HelloCard
{
    public static class HelloCardHelper
    {
        /// <summary>
        /// We look for a few familiar ATRs that represent card types we have additional sample code for.
        /// </summary>
        /// <param name="atr"></param>
        /// <returns></returns>
        public static void PrintCardInfo(byte[] atr)
        {
            string smartCardApiRelatedInfo = "No additional info available (based on ATR)";
            switch (CardHex.FromByteArray(atr))
            {
                case "3B80800101":
                    smartCardApiRelatedInfo = "HID SEOS card. Please refer to HelloSEOS sample code (requires NDA).";
                    break;    
                
                case "3B04A2131091":
                    smartCardApiRelatedInfo = "SLE 4442 detected. Please refer to HelloMemoryCard sample code.";
                    break;

                case "3B0492231091":
                    smartCardApiRelatedInfo = "SLE 4428/5528 detected. Please refer to HelloMemoryCard sample code.";
                    break;

                default:
                    break;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(smartCardApiRelatedInfo);
            Console.ResetColor();
            return ;
        }

         /// <summary>
        /// This method accesses a few popular contactless storage card types or, if available refers to sample code modules
        /// that focus on a given detected storage card.
        /// </summary>
        /// <param name="clicsCard"></param>
        public static void PrintCardInfo(ClicsCard clicsCard)
        {
            // have a look at HelloMifare, HelloNfcTag, HelloICLASS sample code for contactless storage cards
            // we would for example read/write/load keys using NXP Mifare Classic card
            Console.WriteLine(clicsCard.GetVersion());
            Console.WriteLine("contactless storage card detected");
            Console.WriteLine("CardName: " + clicsCard.CardName);
            Console.WriteLine("UID=" + CardHex.FromByteArray(clicsCard.UID)); 
            
            switch (clicsCard.CardName)
            {
                case "TAG_IT":
                    Console.WriteLine("TAG_IT detected");
                    for (int blockIndex = 0; blockIndex < 11; blockIndex++)
                    {
                        byte[] blockData = clicsCard.Read(blockIndex, 4);
                        if (blockData != null)
                        {
                            Console.WriteLine(String.Format("block {0}: {1}", blockIndex.ToString(), CardHex.FromByteArray(blockData)));
                        }
                    }
                    break;

                case "Mifare Ultralight":
                case "Mifare Ultralight C":
                case "Mifare Ultralight EV1":
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Please refer to HelloNfcTag for comprehensive sample code.");
                    Console.ResetColor();
                    break;

                case "Mifare Classic 1k":
                case "Mifare Classic 4k":
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Please refer to HelloMifare for comprehensive sample code.");
                    Console.ResetColor();
                    break;

                case "PicoPass 2K":
                case "PicoPass 2KS":
                case "PicoPass 16K":
                case "PicoPass 16KS":
                case "PicoPass 16K (8x2)":
                case "PicoPass 32KS (16 + 16)":
                case "PicoPass 32KS (16 + 8x2)":
                case "PicoPass 32KS (8x2 + 16)":
                case "PicoPass 32KS (8x2 + 8x2)":
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("HID iCLASS card detected");
                    Console.WriteLine("Please refer to iCLASS sample code (requires NDA).");
                    Console.WriteLine("A Ready-to-use utility (PACSprobe) is available at https://www.PACSprobe.com");
                    Console.ResetColor();
                    break;

                default:
                    break;
            }
        }

    }

}