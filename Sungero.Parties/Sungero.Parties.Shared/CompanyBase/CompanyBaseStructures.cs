using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Parties.Structures.CompanyBase
{
  partial class FoundCompanies
  {
    public string Message { get; set; }
    
    public List<Sungero.Parties.Structures.CompanyBase.CompanyDisplayValue> CompanyDisplayValues { get; set; }
    
    public List<Sungero.Parties.Structures.CompanyBase.FoundContact> FoundContacts { get; set; }
    
    public int Amount { get; set; }
  }
  
  partial class CompanyDisplayValue
  {
    public string DisplayValue { get; set; }
    
    public string PSRN { get; set; }
  }
  
  partial class FoundContact
  {
    public string FullName { get; set; }
    
    public string JobTitle { get; set; }
    
    public string Phone { get; set; }
  }
  
  partial class BusinessEntity
  {
    public string FullName { get; set; }
    
    public string ShortName { get; set; }
  }
}