namespace DEA.Common.Utilities
{
    public partial class Item
    {
      public string Name { get; set; } = string.Empty;
      
      public string Description { get; set; } = string.Empty;
      
      public int Damage { get; set; } = 0;

      public int Accuracy { get; set; } = 0;
      
      public decimal Price { get; set; } = 0;
      
      public bool IsKnife { get; set; } = false;
      
      public bool IsGun { get; set; } = false;
      
      public bool IsArmor { get; set; } = false;                                
    }
}
