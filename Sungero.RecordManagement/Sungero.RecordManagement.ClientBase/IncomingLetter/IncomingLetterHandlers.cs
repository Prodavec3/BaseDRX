using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.RecordManagement.IncomingLetter;

namespace Sungero.RecordManagement
{
  partial class IncomingLetterClientHandlers
  {

    public virtual void SignedByValueInput(Sungero.RecordManagement.Client.IncomingLetterSignedByValueInputEventArgs e)
    {
      this._obj.State.Properties.SignedBy.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void AddresseeValueInput(Sungero.Docflow.Client.IncomingDocumentBaseAddresseeValueInputEventArgs e)
    {
      base.AddresseeValueInput(e);
      this._obj.State.Properties.Addressee.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void SubjectValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      base.SubjectValueInput(e);
      this._obj.State.Properties.Subject.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void PreparedByValueInput(Sungero.Docflow.Client.OfficialDocumentPreparedByValueInputEventArgs e)
    {
      base.PreparedByValueInput(e);
      
      this._obj.State.Properties.PreparedBy.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void InResponseToValueInput(Sungero.Docflow.Client.IncomingDocumentBaseInResponseToValueInputEventArgs e)
    {
      base.InResponseToValueInput(e);
      
      this._obj.State.Properties.InResponseTo.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public virtual void ContactValueInput(Sungero.RecordManagement.Client.IncomingLetterContactValueInputEventArgs e)
    {
      this._obj.State.Properties.Contact.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void Closing(Sungero.Presentation.FormClosingEventArgs e)
    {
      base.Closing(e);
      
      _obj.State.Properties.Subject.IsRequired = false;
      _obj.State.Properties.Correspondent.IsRequired = false;
    }

    public override void InNumberValueInput(Sungero.Presentation.StringValueInputEventArgs e)
    {
      base.InNumberValueInput(e);
      if (Functions.IncomingLetter.HaveDuplicates(_obj, _obj.DocumentKind, _obj.BusinessUnit, e.NewValue, _obj.Dated, _obj.Correspondent))
        e.AddWarning(IncomingLetters.Resources.DuplicateDetected,
                     _obj.Info.Properties.DocumentKind,
                     _obj.Info.Properties.BusinessUnit,
                     _obj.Info.Properties.InNumber,
                     _obj.Info.Properties.Dated,
                     _obj.Info.Properties.Correspondent);
      
      this._obj.State.Properties.InNumber.HighlightColor = Sungero.Core.Colors.Empty;
    }
    
    public override void DocumentKindValueInput(Sungero.Docflow.Client.OfficialDocumentDocumentKindValueInputEventArgs e)
    {
      base.DocumentKindValueInput(e);
      if (Functions.IncomingLetter.HaveDuplicates(_obj, e.NewValue, _obj.BusinessUnit, _obj.InNumber, _obj.Dated, _obj.Correspondent))
        e.AddWarning(IncomingLetters.Resources.DuplicateDetected,
                     _obj.Info.Properties.DocumentKind,
                     _obj.Info.Properties.BusinessUnit,
                     _obj.Info.Properties.InNumber,
                     _obj.Info.Properties.Dated,
                     _obj.Info.Properties.Correspondent);
    }

    public override void BusinessUnitValueInput(Sungero.Docflow.Client.OfficialDocumentBusinessUnitValueInputEventArgs e)
    {
      base.BusinessUnitValueInput(e);
      if (Functions.IncomingLetter.HaveDuplicates(_obj, _obj.DocumentKind, e.NewValue, _obj.InNumber, _obj.Dated, _obj.Correspondent))
        e.AddWarning(IncomingLetters.Resources.DuplicateDetected,
                     _obj.Info.Properties.DocumentKind,
                     _obj.Info.Properties.BusinessUnit,
                     _obj.Info.Properties.InNumber,
                     _obj.Info.Properties.Dated,
                     _obj.Info.Properties.Correspondent);
      
      this._obj.State.Properties.BusinessUnit.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void CorrespondentValueInput(Sungero.Docflow.Client.IncomingDocumentBaseCorrespondentValueInputEventArgs e)
    {
      base.CorrespondentValueInput(e);
      if (Functions.IncomingLetter.HaveDuplicates(_obj, _obj.DocumentKind, _obj.BusinessUnit, _obj.InNumber, _obj.Dated, e.NewValue))
        e.AddWarning(IncomingLetters.Resources.DuplicateDetected,
                     _obj.Info.Properties.DocumentKind,
                     _obj.Info.Properties.BusinessUnit,
                     _obj.Info.Properties.InNumber,
                     _obj.Info.Properties.Dated,
                     _obj.Info.Properties.Correspondent);
      
      this._obj.State.Properties.Correspondent.HighlightColor = Sungero.Core.Colors.Empty;
      this._obj.State.Properties.Contact.HighlightColor = Sungero.Core.Colors.Empty;
      this._obj.State.Properties.SignedBy.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void DatedValueInput(Sungero.Presentation.DateTimeValueInputEventArgs e)
    {
      base.DatedValueInput(e);
      
      if (e.NewValue != null && e.NewValue >= Calendar.SqlMinValue)
      {
        if (Functions.IncomingLetter.HaveDuplicates(_obj, _obj.DocumentKind, _obj.BusinessUnit, _obj.InNumber, e.NewValue, _obj.Correspondent))
          e.AddWarning(IncomingLetters.Resources.DuplicateDetected,
                       _obj.Info.Properties.DocumentKind,
                       _obj.Info.Properties.BusinessUnit,
                       _obj.Info.Properties.InNumber,
                       _obj.Info.Properties.Dated,
                       _obj.Info.Properties.Correspondent);
      }
      
      // Для DateTime событие изменения отрабатывает, даже если даты одинаковые.
      // Поэтому еще раз сравниваем только даты без учёта времени.
      if (e.OldValue.HasValue && e.NewValue.HasValue && Equals(e.OldValue.Value.Date, e.NewValue.Value.Date))
        return;
      
      this._obj.State.Properties.Dated.HighlightColor = Sungero.Core.Colors.Empty;
    }

    public override void Refresh(Sungero.Presentation.FormRefreshEventArgs e)
    {
      base.Refresh(e);
      var isCorrespondent = _obj.Correspondent == null || Sungero.Parties.CompanyBases.Is(_obj.Correspondent);
      _obj.State.Properties.SignedBy.IsEnabled = isCorrespondent;
      _obj.State.Properties.Contact.IsEnabled = isCorrespondent;
    }
  }
}