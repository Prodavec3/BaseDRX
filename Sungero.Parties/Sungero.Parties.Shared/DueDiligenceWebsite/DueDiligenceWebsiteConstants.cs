using System;
using Sungero.Core;

namespace Sungero.Parties.Constants
{
  public static class DueDiligenceWebsite
  {
    public static class Initialize
    {
      public static readonly Guid ForFairBusinessWebsite = Guid.Parse("99e9327e-efb7-4510-a556-fbccdcaece72");
      public static readonly Guid HonestBusinessWebsite = Guid.Parse("65963469-d16b-4352-a1ab-6d06a635f028");
      public static readonly Guid KonturFocusWebsite = Guid.Parse("ad8b8147-65db-4fd4-9cdb-1a7b189ddb4e");
      public static readonly Guid OwnerOnlineWebsite = Guid.Parse("9d3c0d6a-d958-4c9c-8ad5-5d1dd4eacf6b");
      public static readonly Guid SbisWebsite = Guid.Parse("42017695-b8fa-4207-ba3a-051cf73835d5");
    }
    
    public static class Websites
    {
      public const string InnMask = @"{INN}";
      public const string OgrnMask = @"{OGRN}";
      
      public static class ForFairBusiness
      {
        public const string DueDiligencePage = @"https://zachestnyibiznes.ru/company/ul/" + OgrnMask;
        public const string DueDiligencePageSE = @"https://zachestnyibiznes.ru/company/ip/" + OgrnMask;
        public const string HomePage = @"https://zachestnyibiznes.ru";
      }
      
      public static class HonestBusiness
      {
        public const string DueDiligencePage = @"https://honestbusiness.ru/id" + OgrnMask;
        public const string HomePage = @"https://honestbusiness.ru";
      }
      
      public static class KonturFocus
      {
        public const string DueDiligencePage = @"https://focus.kontur.ru/entity?query=" + OgrnMask;
        public const string HomePage = @"https://focus.kontur.ru";
      }
      
      public static class OwnerOnline
      {
        public const string DueDiligencePage = @"https://kontragent.io/search/" + OgrnMask;
        public const string HomePage = @"https://kontragent.io";
      }
      
      public static class Sbis
      {
        public const string DueDiligencePage = @"https://sbis.ru/contragents/" + InnMask;
        public const string HomePage = @"https://sbis.ru/contragents";
      }
    }
  }
}