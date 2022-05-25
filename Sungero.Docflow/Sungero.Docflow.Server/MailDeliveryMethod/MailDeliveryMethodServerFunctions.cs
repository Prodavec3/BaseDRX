using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.MailDeliveryMethod;

namespace Sungero.Docflow.Server
{
  partial class MailDeliveryMethodFunctions
  {
    /// <summary>
    /// Получить способ доставки "Сервис эл. обмена".
    /// </summary>
    /// <returns>Способ доставки "Сервис эл. обмена".</returns>
    [Remote(IsPure = true), Public]
    public static IMailDeliveryMethod GetExchangeDeliveryMethod()
    {
      return MailDeliveryMethods.GetAll(x => x.Sid == Constants.MailDeliveryMethod.Exchange).FirstOrDefault();
    }
  }
}