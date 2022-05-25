using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.ApprovalSendingAssignment;

namespace Sungero.Docflow
{
  partial class ApprovalSendingAssignmentSharedHandlers
  {

    public virtual void ExchangeServiceChanged(Sungero.Docflow.Shared.ApprovalSendingAssignmentExchangeServiceChangedEventArgs e)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var isManyAddressees = OutgoingDocumentBases.Is(document) ? OutgoingDocumentBases.As(document).IsManyAddressees.Value : false;
      _obj.DeliveryMethodDescription = Functions.ApprovalTask.GetDeliveryMethodDescription(_obj.DeliveryMethod, e.NewValue, isManyAddressees);
    }

    public virtual void DeliveryMethodChanged(Sungero.Docflow.Shared.ApprovalSendingAssignmentDeliveryMethodChangedEventArgs e)
    {
      var document = _obj.DocumentGroup.OfficialDocuments.FirstOrDefault();
      var isManyAddressees = OutgoingDocumentBases.Is(document) ? OutgoingDocumentBases.As(document).IsManyAddressees.Value : false;
      _obj.DeliveryMethodDescription = Functions.ApprovalTask.GetDeliveryMethodDescription(e.NewValue, _obj.ExchangeService, isManyAddressees);
    }

  }
}