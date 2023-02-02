namespace Apps.Communication.Profinet.LSIS
{
	public enum XGT_MemoryType
	{
		/// <summary>입출력(Bit)</summary>
		IO,
		/// <summary>보조릴레이(Bit)</summary>
		SubRelay,
		/// <summary>링크릴레이(Bit)</summary>
		LinkRelay,
		/// <summary>Keep릴레이(Bit)</summary>
		KeepRelay,
		/// <summary>특수릴레이(Bit)</summary>
		EtcRelay,
		/// <summary>타이머(현재값)(Word)</summary>
		Timer,
		/// <summary>카운터(현재값)(Word)</summary>
		Counter,
		DataRegister,
		/// <summary>통신 데이터레지스터(Word)</summary>
		ComDataRegister,
		/// <summary>파일 레지스터(Word)</summary>
		FileDataRegister,
		/// <summary>파일 레지스터(Word)</summary>
		StepRelay,
		/// <summary>파일 레지스터(Word)</summary>
		SpecialRegister
	}
}
