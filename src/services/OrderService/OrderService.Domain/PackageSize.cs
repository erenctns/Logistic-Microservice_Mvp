namespace SmartLogistics.OrderService.Domain;

// Paket boyutu. Su an yalnizca tasiniyor ve saklaniyor; bir kurye secim
// algoritmasi eklenirse karar girdisi olur (buyuk paket motosikletle tasinamaz).
public enum PackageSize
{
    Small = 0,
    Medium = 1,
    Large = 2,
}
