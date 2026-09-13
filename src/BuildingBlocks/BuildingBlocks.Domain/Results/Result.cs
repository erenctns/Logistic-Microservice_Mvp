namespace SmartLogistics.BuildingBlocks.Domain.Results;

// Bir islemin sonucu: ya basarili, ya da bir Error tasiyor.
//
// Neden exception degil? Is kurali ihlali ("stok yetersiz") beklenmedik bir durum
// degildir; olagan bir sonuctur. Exception'i gercekten beklenmedik seylere
// (DB dustu, disk doldu) sakliyoruz. Ayrica metot imzasi durust olur:
// "Result donduruyorsam basarisiz olabilirim" demektir.
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        // Tutarsiz durumu kurulusta engelle: basarili ama hatali, ya da
        // basarisiz ama hatasiz bir sonuc programci hatasidir.
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Basarili sonuc hata tasiyamaz.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Basarisiz sonuc bir hata tasimalidir.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

// Deger tasiyan sonuc. Basarisizsa Value okunamaz — bu bilerek boyle:
// hatayi kontrol etmeden degeri kullanmak istendiginde sessizce null donmek yerine
// yuksek sesle patlamasi daha iyidir.
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Basarisiz sonucun degeri okunamaz.");

    // Ortuk donusum: "return order;" yazmak "return Result.Success(order);" ile ayni.
    // Cagri yerindeki gurultuyu azaltir.
    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
