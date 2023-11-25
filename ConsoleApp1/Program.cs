using ConsoleApp1;
using System.Diagnostics;
using System.Reflection;

try
{
	ConsumeDisposable();
}
catch (Exception ex)
{
	Console.WriteLine(ex.ToString());
}

static void ConsumeDisposable()
{
	using var disposable = GetDisposable();
}

static Disposable1 GetDisposable()
{
	var disposable = new Disposable1();
	return disposable;
}