using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Memo;

namespace Sungero.Docflow.Server
{
  partial class MemoFunctions
  {
    /// <summary>
    /// Заполнить подписывающего в карточке документа.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    [Remote]
    public override void SetDocumentSignatory(Company.IEmployee employee)
    {
      // Не перебивать подписанта при рассмотрении. US: 78188.
      var documentParams = ((Domain.Shared.IExtendedEntity)_obj).Params;
      var calledFromApprovalReviewAssignments = documentParams.ContainsKey(Constants.ApprovalReviewAssignment.CalledFromApprovalReviewAssignments);
      
      if (CallContext.CalledFrom(ApprovalReviewAssignments.Info) || calledFromApprovalReviewAssignments)
        return;

      base.SetDocumentSignatory(employee);
    }
    
    /// <summary>
    /// Заполнить основание в карточке документа.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <param name="e">Аргументы события подписания.</param>
    /// <param name="changedSignatory">Признак смены подписывающего.</param>
    public override void SetOurSigningReason(Company.IEmployee employee, Sungero.Domain.BeforeSigningEventArgs e, bool changedSignatory)
    {
      // Не перебивать основание при рассмотрении, если подписывает Адресат. US: 189445.
      var documentParams = ((Domain.Shared.IExtendedEntity)_obj).Params;
      var calledFromApprovalReviewAssignments = documentParams.ContainsKey(Constants.ApprovalReviewAssignment.CalledFromApprovalReviewAssignments);
      
      if ((CallContext.CalledFrom(ApprovalReviewAssignments.Info) || calledFromApprovalReviewAssignments) && !Equals(_obj.OurSignatory, employee))
        return;

      base.SetOurSigningReason(employee, e, changedSignatory);
    }
    
    /// <summary>
    /// Заполнить Единый рег. № из эл. доверенности в подпись.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    /// <param name="signature">Подпись.</param>
    /// <param name="certificate">Сертификат для подписания.</param>
    public override void SetUnifiedRegistrationNumber(Company.IEmployee employee, Sungero.Domain.Shared.ISignature signature, ICertificate certificate)
    {
      if (signature.SignCertificate == null)
        return;
      
      var documentParams = ((Domain.Shared.IExtendedEntity)_obj).Params;
      var calledFromApprovalReviewAssignments = documentParams.ContainsKey(Constants.ApprovalReviewAssignment.CalledFromApprovalReviewAssignments);
      
      if (CallContext.CalledFrom(ApprovalReviewAssignments.Info) || calledFromApprovalReviewAssignments)
      {
        var signatureSettingsByEmployee = this.GetSignatureSettingsByEmployee(employee).ToList();
        var ourSigningReasonAddressee = PublicFunctions.Module.GetOurSigningReasonWithHighPriority(signatureSettingsByEmployee, certificate);
          
        this.SetUnifiedRegistrationNumber(ourSigningReasonAddressee, signature, certificate);
        return;
      }

      base.SetUnifiedRegistrationNumber(employee, signature, certificate);
    }
    
    /// <summary>
    /// Признак того, что необходимо проверять наличие прав подписи на документ у сотрудника, указанного в качестве подписанта с нашей стороны.
    /// </summary>
    /// <returns>True - необходимо проверять, False - иначе.</returns>
    /// <remarks>Проверка прав подписи не проводится для служебной записки.</remarks>
    public override bool NeedValidateOurSignatorySignatureSetting()
    {
      return false;
    }
    
    /// <summary>
    /// Заполнить текстовое отображение адресатов.
    /// </summary>
    public virtual void SetManyAddresseesLabel()
    {
      var addressees = _obj.Addressees
        .Where(x => x.Addressee != null)
        .Select(x => x.Addressee)
        .ToList();
      var maxLength = _obj.Info.Properties.ManyAddresseesLabel.Length;
      var label = Functions.Module.BuildManyAddresseesLabel(addressees, maxLength);
      if (_obj.ManyAddresseesLabel != label)
        _obj.ManyAddresseesLabel = label;
    }

    /// <summary>
    /// Получить краткий список адресатов для шаблона служебной записки.
    /// </summary>
    /// <param name="document">Служебная записка.</param>
    /// <returns>Список адресатов.</returns>
    [Sungero.Core.Converter("AddresseesShortList")]
    public static string AddresseesShortList(IMemo document)
    {
      return Functions.Memo.GetAddresseesShortList(document);
    }

    /// <summary>
    /// Получить краткий список адресатов для шаблона служебной записки.
    /// </summary>
    /// <returns>Список адресатов.</returns>
    /// <remarks>Формат:
    /// <para>Должность (в дательном падеже)</para>
    /// <para>Фамилия И.О. (в дательном падеже).</para></remarks>
    public virtual string GetAddresseesShortList()
    {
      var addressees = _obj.Addressees
        .Where(x => x != null)
        .OrderBy(x => x.Number)
        .Select(x => x.Addressee)
        .Distinct()
        .ToList();
      if (addressees.Count() > Constants.Module.AddresseesShortListLimit)
        return OfficialDocuments.Resources.ToManyAddressees;
      
      var addresseesList = new List<string>();
      foreach (var addressee in addressees)
      {
        if (addressee == null)
          continue;
        
        // Должность адресата в дательном падеже.
        var jobTitle = addressee.JobTitle != null && !string.IsNullOrEmpty(addressee.JobTitle.Name)
          ? CaseConverter.ConvertJobTitleToTargetDeclension(addressee.JobTitle.Name, Sungero.Core.DeclensionCase.Dative)
          : string.Format("<{0}>", OfficialDocuments.Resources.JobTitle);
        
        // Фамилия И.О. адресата в дательном падеже.
        var addresseeName = Company.PublicFunctions.Employee.GetShortName(addressee,
                                                                          Sungero.Core.DeclensionCase.Dative,
                                                                          false);
        
        var nameWithJobTitle = string.Join(Environment.NewLine, jobTitle, addresseeName, string.Empty);
        addresseesList.Add(nameWithJobTitle);
      }

      return string.Join(Environment.NewLine, addresseesList).Trim();
    }
    
    /// <summary>
    /// Получить полный список адресатов для шаблона служебной записки.
    /// </summary>
    /// <param name="document">Служебная записка.</param>
    /// <returns>Список адресатов.</returns>
    [Sungero.Core.Converter("AddresseesFullList")]
    public static string AddresseesFullList(IMemo document)
    {
      return Functions.Memo.GetAddresseesFullList(document);
    }

    /// <summary>
    /// Получить полный список адресатов для шаблона служебной записки.
    /// </summary>
    /// <returns>Список адресатов.</returns>
    /// <remarks>Формат:
    /// <para>Номер по порядку. Фамилия И.О. - Должность.</para></remarks>
    public virtual string GetAddresseesFullList()
    {
      var addressees = _obj.Addressees
        .Where(x => x != null)
        .OrderBy(x => x.Number)
        .Select(x => x.Addressee)
        .Distinct()
        .ToList();
      return Company.PublicFunctions.Employee.Remote.GetEmployeesNumberedList(addressees, true, false);
    }
  }
}