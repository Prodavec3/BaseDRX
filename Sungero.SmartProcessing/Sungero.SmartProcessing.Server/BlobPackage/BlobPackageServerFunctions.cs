using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.SmartProcessing.BlobPackage;

namespace Sungero.SmartProcessing.Server
{
  partial class BlobPackageFunctions
  {
    
    /// <summary>
    /// Создать пакет блобов обрабатываемых документов.
    /// </summary>
    /// <returns>Пакет блобов.</returns>
    [Remote, Public]
    public static IBlobPackage CreateBlobPackage()
    {
      return BlobPackages.Create();
    }
    
    /// <summary>
    /// Заполнить в пакете бинарных образов документов информацию о письме, находящуюся в пакете бинарных образов документов DCS.
    /// </summary>
    /// <param name="source">Пакет бинарных образов документов DCS.</param>
    [Public]
    public virtual void FillMailInfoFromDcsPackage(Structures.Module.IDcsPackage source)
    {
      var mailInfo = source.MailInfo;
      if (mailInfo == null)
        return;
      
      // Тема.
      var subject = mailInfo.Subject;
      if (!string.IsNullOrWhiteSpace(subject) &&
          subject.Length > _obj.Info.Properties.Subject.Length)
      {
        subject = subject.Substring(0, _obj.Info.Properties.Subject.Length);
      }
      _obj.Subject = subject;
      
      // От кого.
      _obj.FromAddress = mailInfo.FromAddress;
      _obj.FromName = mailInfo.FromName;
      
      // Кому.
      foreach (var recipient in mailInfo.To)
      {
        var mailToRecipient = _obj.To.AddNew();
        mailToRecipient.Name = recipient.Name;
        mailToRecipient.Address = recipient.Address;
      }
      
      // Копия.
      foreach (var recipient in mailInfo.CC)
      {
        var copyRecipient = _obj.CC.AddNew();
        copyRecipient.Name = recipient.Name;
        copyRecipient.Address = recipient.Address;
      }
      
      _obj.MessageId = mailInfo.MessageId;
      _obj.Priority = mailInfo.Priority;
      _obj.SendDate = mailInfo.SendDate.FromUtcTime();
    }

    /// <summary>
    /// Создать информацию о письме, находящуюся в пакете бинарных образов документов.
    /// </summary>
    /// <returns>Информация о письме.</returns>
    [Public]
    public virtual Structures.Module.IMailInfo CreateMailInfo()
    {
      var mailInfo = Structures.Module.MailInfo.Create();
      
      // Тема.
      mailInfo.Subject = _obj.Subject;
      
      // От кого.
      mailInfo.FromAddress = _obj.FromAddress;
      mailInfo.FromName = _obj.FromName;
      
      // Кому.
      mailInfo.To = new List<Structures.Module.IMailRecipient>();
      foreach (var recipient in _obj.To)
      {
        var mailToRecipient = Structures.Module.MailRecipient.Create();
        mailToRecipient.Name = recipient.Name;
        mailToRecipient.Address = recipient.Address;
        mailInfo.To.Add(mailToRecipient);
      }
      
      // Копия.
      mailInfo.CC = new List<Structures.Module.IMailRecipient>();
      foreach (var recipient in _obj.CC)
      {
        var copyRecipient = Structures.Module.MailRecipient.Create();
        copyRecipient.Name = recipient.Name;
        copyRecipient.Address = recipient.Address;
        mailInfo.CC.Add(copyRecipient);
      }
      
      mailInfo.MessageId = _obj.MessageId;
      mailInfo.Priority = _obj.Priority;
      mailInfo.SendDate = _obj.SendDate;
      
      return mailInfo;
    }
  }
}