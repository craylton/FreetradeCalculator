namespace FreetradeCalculator.Tax;

public readonly record struct TaxYear(int StartYear) : IComparable<TaxYear>
{
	public static TaxYear From(DateTimeOffset timestamp)
	{
		int startYear = timestamp.Month > 4 || (timestamp.Month == 4 && timestamp.Day >= 6)
			? timestamp.Year
			: timestamp.Year - 1;

		return new TaxYear(startYear);
	}

	public int CompareTo(TaxYear other) => StartYear.CompareTo(other.StartYear);

	public override string ToString() => $"{StartYear}/{(StartYear + 1) % 100:00}";
}
