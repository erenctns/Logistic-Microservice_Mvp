namespace SmartLogistics.OrderService.Domain;

// Paket boyutu. Step 11'de kurye secim algoritmasinda kullanilacak:
// buyuk paket motosikletle tasinamaz.
public enum PackageSize
{
    Small = 0,
    Medium = 1,
    Large = 2,
}
