using System;

public class ConsistentRandom : Random
{
	private const int MBIG = 2147483647;

	private const int MSEED = 161803398;

	private const int MZ = 0;

	private int inext;

	private int inextp;

	private int[] SeedArray;

	public ConsistentRandom() : this(Environment.TickCount)
	{
	}

	public ConsistentRandom(int seed)
	{
		SeedArray = new int[56];
		int subtraction = seed == int.MinValue ? int.MaxValue : Math.Abs(seed);
		int mj = MSEED - subtraction;
		SeedArray[55] = mj;
		int mk = 1;
		for (int i = 1; i < 55; i++)
		{
			int index = (21 * i) % 55;
			SeedArray[index] = mk;
			mk = mj - mk;
			if (mk < 0) mk += MBIG;
			mj = SeedArray[index];
		}
		for (int pass = 1; pass < 5; pass++)
		for (int i = 1; i < 56; i++)
		{
			SeedArray[i] -= SeedArray[1 + (i + 30) % 55];
			if (SeedArray[i] < 0) SeedArray[i] += MBIG;
		}
		inext = 0;
		inextp = 21;
	}

	protected override double Sample()
	{
		return InternalSample() * (1.0 / MBIG);
	}

	private int InternalSample()
	{
		int next = inext + 1;
		if (next >= 56) next = 1;
		int nextp = inextp + 1;
		if (nextp >= 56) nextp = 1;
		int result = SeedArray[next] - SeedArray[nextp];
		if (result == MBIG) result--;
		if (result < 0) result += MBIG;
		SeedArray[next] = result;
		inext = next;
		inextp = nextp;
		return result;
	}

	public override int Next()
	{
		return InternalSample();
	}

	private double GetSampleForLargeRange()
	{
		int result = InternalSample();
		if (InternalSample() % 2 == 0) result = -result;
		return (result + (double)(int.MaxValue - 1)) / (2.0 * int.MaxValue - 1);
	}

	public override int Next(int minValue, int maxValue)
	{
		if (minValue > maxValue) throw new ArgumentOutOfRangeException(nameof(minValue));
		long range = (long)maxValue - minValue;
		return range <= int.MaxValue
			? (int)(Sample() * range) + minValue
			: (int)((long)(GetSampleForLargeRange() * range) + minValue);
	}

	public override void NextBytes(byte[] buffer)
	{
		if (buffer == null) throw new ArgumentNullException(nameof(buffer));
		for (int i = 0; i < buffer.Length; i++) buffer[i] = (byte)InternalSample();
	}
}
