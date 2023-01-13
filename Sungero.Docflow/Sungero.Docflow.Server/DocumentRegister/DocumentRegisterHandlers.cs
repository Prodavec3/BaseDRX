using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentRegister;

namespace Sungero.Docflow
{
  partial class DocumentRegisterRegistrationGroupPropertyFilteringServerHandler<T>
  {
    public virtual IQueryable<T> RegistrationGroupFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e)
    {
      if (_obj.DocumentFlow.HasValue)
      {
        if (_obj.DocumentFlow == DocumentFlow.Incoming)
          query = query.Where(g => g.CanRegisterIncoming == true);
        if (_obj.DocumentFlow == DocumentFlow.Inner)
          query = query.Where(g => g.CanRegisterInternal == true);
        if (_obj.DocumentFlow == DocumentFlow.Outgoing)
          query = query.Where(g => g.CanRegisterOutgoing == true);
        if (_obj.DocumentFlow == DocumentFlow.Contracts)
          query = query.Where(g => g.CanRegisterContractual == true);
      }
      
      if (Functions.Module.IsAdministratorOrAdvisor())
        return query;
      
      var clerks = Functions.DocumentRegister.GetClerks();
      if (clerks == null)
        return query;
      var allRecipientIds = Recipients.AllRecipientIds;
      if (allRecipientIds.Contains(clerks.Id))
        query = query.Where(g => g.RecipientLinks.Any(l => allRecipientIds.Contains(l.Member.Id)));
      return query;
    }
  }

  partial class DocumentRegisterCreatingFromServerHandler
  {

    public override void CreatingFrom(Sungero.Domain.CreatingFromEventArgs e)
    {
      e.Without(_info.Properties.Name);
      e.Without(_info.Properties.Index);
    }
  }

  partial class DocumentRegisterServerHandlers
  {

    public override void BeforeSave(Sungero.Domain.BeforeSaveEventArgs e)
    {
      var hasDocuments = Functions.DocumentRegister.HasRegisteredDocuments(_obj);
      var isUsed = hasDocuments || Functions.Module.GetRegistrationSettingByDocumentRegister(_obj).Any();
      if (isUsed && _obj.State.Properties.RegistrationGroup.IsChanged)
        e.AddError(DocumentRegisters.Resources.NoRightToChangePropertyFormat(_obj.Info.Properties.RegistrationGroup.LocalizedName));
      if (isUsed && _obj.State.Properties.RegisterType.IsChanged)
        e.AddError(DocumentRegisters.Resources.NoRightToChangePropertyFormat(_obj.Info.Properties.RegisterType.LocalizedName));
      if (isUsed && _obj.State.Properties.DocumentFlow.IsChanged)
        e.AddError(DocumentRegisters.Resources.NoRightToChangePropertyFormat(_obj.Info.Properties.DocumentFlow.LocalizedName));
      if (hasDocuments && (_obj.State.Properties.NumberingPeriod.IsChanged || _obj.State.Properties.NumberingSection.IsChanged))
        e.AddError(DocumentRegisters.Resources.NoRightToChangePropertyFormat(_obj.Info.Properties.NumberingSection.LocalizedName));
      
      if (_obj.RegistrationGroup != null &&
          !Recipients.AllRecipientIds.Contains(_obj.RegistrationGroup.Id) &&
          !Recipients.OwnRecipientIds.Contains(Roles.Administrators.Id))
      {
        e.AddError(DocumentRegisters.Resources.NoRightToChange);
        return;
      }
      
      // Проверить наличие пробелов в номере.
      if (_obj.NumberFormatItems.Any(x => x.Separator != null && Regex.IsMatch(x.Separator, @"\s")))
      {
        e.AddError(DocumentRegisters.Resources.NoSpaces);
        return;
      }
      
      var format = _obj.NumberFormatItems;
      
      // Проверить наличие порядкового номера в формате номера.
      var numberElements = format.Where(f => f.Element == DocumentRegisterNumberFormatItems.Element.Number);
      if (!numberElements.Any())
        e.AddError(DocumentRegisters.Resources.NoNumberInNumberFormat);
      else if (numberElements.Count() > 1)
        e.AddError(DocumentRegisters.Resources.InNumbersFormatShouldBeNotMoreOneNumbers);
          
      // Проверить уникальность номера элемента в формате номера.
      if (format.Any(f => format.Any(d => d.Element == f.Element && d.Number != f.Number)))
        e.AddError(DocumentRegisters.Resources.FormatElementNumbersIsNotUnique);
      
      if (format.Any(f => format.Any(d => d.Number == f.Number && d.Id != f.Id)))
        e.AddError(DocumentRegisters.Resources.NotUniqueNumber);
      
      // Проверить наличие и положение начала строки в формате номера.
      var beginningOfLine = format.Where(f => f.Element == DocumentRegisterNumberFormatItems.Element.BegginingOfLine).FirstOrDefault();
      if (beginningOfLine != null &&
          format.OrderBy(f => f.Number).Select(f => f.Number).FirstOrDefault() != beginningOfLine.Number)
        e.AddError(DocumentRegisters.Resources.BegginingOfLineError);
      
      // Если в формате номера есть индекс группы регистрации, то проверять, что журнал регистрации заполнен.
      if (_obj.NumberFormatItems.Select(x => x.Element).Contains(Docflow.DocumentRegisterNumberFormatItems.Element.RegistrPlace) && _obj.RegistrationGroup == null)
        e.AddError(DocumentRegisters.Resources.ForDocumentNumberMustBeAttendRegisterGroup);
      
      _obj.DisplayName = string.Format("{0}. {1}", _obj.Index, _obj.Name);
    }

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      // Заполнить формат номера по умолчанию началом строки и порядковым номером.
      if (!_obj.NumberFormatItems.Any())
      {
        var beginningOfLine = _obj.NumberFormatItems.AddNew();
        beginningOfLine.Number = 0;
        beginningOfLine.Element = DocumentRegisterNumberFormatItems.Element.BegginingOfLine;
        
        var number = _obj.NumberFormatItems.AddNew();
        number.Number = 1;
        number.Element = DocumentRegisterNumberFormatItems.Element.Number;
      }
      
      if (!_obj.State.IsCopied)
      {
        _obj.RegisterType = RegisterType.Registration;
        _obj.NumberingPeriod = NumberingPeriod.Year;
        _obj.NumberingSection = NumberingSection.NoSection;
      }
      
      if (!_obj.RegisterType.Equals(DocumentRegister.RegisterType.Numbering))
      {
        var userRegistrationGroups = Functions.DocumentRegister.GetUsersRegistrationGroups(_obj);
        if (userRegistrationGroups.Count() == 1)
          _obj.RegistrationGroup = userRegistrationGroups.FirstOrDefault();
      }
    }
  }
}