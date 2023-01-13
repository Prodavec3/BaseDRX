using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.Parties.Shared
{
  public class ModuleFunctions
  {
    #region Email
    
    /// <summary>
    /// Проверить email на валидность.
    /// </summary>
    /// <param name="emailAddress">Email.</param>
    /// <returns>Признак валидности email.</returns>
    [Public]
    public static bool EmailIsValid(string emailAddress)
    {
      try
      {
        MailAddress email = new MailAddress(emailAddress);
      }
      catch (FormatException)
      {
        return false;
      }
      return true;
    }

    #endregion

    /// <summary>
    /// Получить фамилию и инициалы в культуре тенанта.
    /// </summary>
    /// <param name="firstName">Имя.</param>
    /// <param name="middleName">Отчество.</param>
    /// <param name="lastName">Фамилия.</param>
    /// <returns>ФИО в коротком формате в локали тенанта.</returns>
    [Public]
    public virtual string GetSurnameAndInitialsInTenantCulture(string firstName, string middleName, string lastName)
    {
      using (TenantInfo.Culture.SwitchTo())
      {
        if (string.IsNullOrWhiteSpace(firstName))
          return lastName;
        
        if (string.IsNullOrWhiteSpace(middleName))
          return People.Resources.ShortNameWithoutMiddleFormat(firstName.ToUpper()[0], lastName, "\u00A0");
        
        return People.Resources.ShortNameFormat(firstName.ToUpper()[0], middleName.ToUpper()[0], lastName, "\u00A0");
      }
    }
    
  }
}