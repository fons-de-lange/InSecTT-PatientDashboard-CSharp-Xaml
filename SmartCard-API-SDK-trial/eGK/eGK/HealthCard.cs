// --------------------------------------------------------------------------------------------
// HealthCard.cs
// German Healthcard sample code for CardWerk SmartCard API (professional)
// Copyright © 2004-2014 CardWerk Technologies
// --------------------------------------------------------------------------------------------

namespace GermanHealthInsuranceCard
{
	using System;

	/// <summary>
	/// Abstract base class for all German health insurance cards.
	/// </summary>

	public abstract class HealthCard
	{
		protected string m_sInsuranceName;
		protected string m_sInsuranceNumber;

		protected string m_sInsuredNumber;
		protected string m_sTitleName;
		protected string m_sFirstName;
		protected string m_sLastName;
		protected string m_sStreet;
		protected string m_sZipCode;
		protected string m_sCity;
		protected DateTime m_tBirthday;

		/// <summary>
		/// Resets all class members to their default values. Use this method whenever you are starting
        /// a new session with a card.
		/// </summary>

		protected void Clear()
		{
			m_sInsuranceName = null;
			m_sInsuranceNumber = null;
			m_sTitleName = null;
			m_sFirstName = null;
			m_sLastName = null;
			m_sStreet = null;
			m_sZipCode = null;
			m_sCity = null;
			m_tBirthday = DateTime.MinValue;
		}

		/// <summary>
		/// This method must be overridden and should reflect a friendly name 
        /// such as KVK, Krankenversicherungskarte, eGK or elektronische Gesundheitskarte.
		/// </summary>

		public abstract string CardName { get; }

		/// <summary>
		/// 
		/// </summary>

		public string InsuranceName
		{
			get
			{
				return m_sInsuranceName;
			}
		}

		/// <summary>
		/// 
		/// </summary>

		public string InsuranceNumber
		{
			get
			{
				return m_sInsuranceNumber;
			}
		}

		/// <summary>
		/// 
		/// </summary>

		public string InsuredNumber
		{
			get
			{
				return m_sInsuredNumber;
			}
		}

		/// <summary>
		/// 
		/// </summary>

		public string TitleName
		{
			get
			{
				return m_sTitleName;
			}
		}

		/// <summary>
		/// 
		/// </summary>

		public string FirstName
		{
			get
			{
				return m_sFirstName;
			}
		}

		/// <summary>
		/// 
		/// </summary>

		public string LastName
		{
			get
			{
				return m_sLastName;
			}
		}

		/// <summary>
		/// 
		/// </summary>

		public string Street
		{
			get
			{
				return m_sStreet;
			}
		}

		/// <summary>
		/// 
		/// </summary>

		public string ZipCode
		{
			get
			{
				return m_sZipCode;
			}
		}

		/// <summary>
		/// 
		/// </summary>

		public string City
		{
			get
			{
				return m_sCity;
			}
		}

		/// <summary>
		/// 
		/// </summary>

		public DateTime Birthday
		{
			get
			{
				return m_tBirthday;
			}
		}
	}
}