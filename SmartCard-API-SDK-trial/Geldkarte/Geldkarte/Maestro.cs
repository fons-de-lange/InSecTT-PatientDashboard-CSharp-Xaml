// --------------------------------------------------------------------------------------------
// Maestro.cs
// CardWerk SmartCard API
// Copyright © 2004-2011 CardWerk Technologies
// --------------------------------------------------------------------------------------------

using System;
using Subsembly.SmartCard;

namespace HelloGeldkarte
{
	/// <summary>
	/// This is a simple example for a card service implementation for the Maestro payment
	/// system.
	/// </summary>

	public class Maestro
	{
		CardHandle m_aCard;

		/// <summary>
		/// Attempts to select the Maestro application on the given card,
		/// </summary>
		/// <param name="aCard"></param>
		/// <returns>
		/// If the Maestro application was successfully selected, then this class takes
		/// over the ownership of the given CardHandle and returns <c>true</c> to indicate
		/// success. In this case, the caller is responsible to ultimately release the card
		/// again via <see cref="Release"/>.
		/// If the Maestro could not be selected, then <c>false</c> is returned.
		/// </returns>

		public bool Select(CardHandle aCard)
		{
			if (aCard == null)
			{
				throw new ArgumentNullException();
			}
			if (m_aCard != null)
			{
				throw new InvalidOperationException();
			}

			CardResponseAPDU aRespAPDU;

			CardCommandAPDU aSelectApplicationAPDU = new CardCommandAPDU(
				aCard.CLA, 0xA4, 0x04, 0x00, CardHex.ToByteArray("A0000000043060"), 0);

			aRespAPDU = aCard.SendCommand(aSelectApplicationAPDU);
			if (aRespAPDU.IsSuccessful)
			{
				m_aCard = aCard;
				return true;
			}

			return false;
		}

		/// <summary>
		/// 
		/// </summary>

		public void Release()
		{
			if (m_aCard != null)
			{
				m_aCard.Dispose();
				m_aCard = null;
			}
		}
	}
}
