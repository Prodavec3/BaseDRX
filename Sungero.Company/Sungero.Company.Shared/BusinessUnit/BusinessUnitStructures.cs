using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Company.Structures.BusinessUnit
{
  partial class BusinessUnitForRelations
  {
    public int Id { get; set; }
    
    public Sungero.Company.IBusinessUnit HeadCompany { get; set; }
  }
}