using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.Person;

namespace Sungero.Parties
{
  partial class PersonSharedHandlers
  {

    public virtual void MiddleNameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      Functions.Person.FillName(_obj);
    }

    public virtual void FirstNameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      Functions.Person.FillName(_obj);
    }

    public virtual void LastNameChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // Установить пол, если не установлен.
      // TODO: 35010.
      if (System.Threading.Thread.CurrentThread.CurrentUICulture.Equals(System.Globalization.CultureInfo.CreateSpecificCulture("ru-RU")))
      {
        if (!_obj.Sex.HasValue && e.NewValue != null && e.NewValue.Length > 2)
        {
          var lastSymbols = e.NewValue.Substring(e.NewValue.Length - 2).ToLower();
          if (lastSymbols == "ва" || lastSymbols == "на" || lastSymbols == "ая")
            _obj.Sex = Sex.Female;
          else
            _obj.Sex = Sex.Male;
        }
      }
      
      Functions.Person.FillName(_obj);
    }
  }
}