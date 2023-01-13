-- Заполнить свойство AllowApproveWithSuggestions значением по умолчанию.
-- bbb34726-4316-4d87-a64d-9dcf74b654f4 - GUID ApprovalStage
update sungero_docflow_approvalstage
set allowapprwsugg = 'false'
where allowapprwsugg is null
and discriminator = 'bbb34726-4316-4d87-a64d-9dcf74b654f4';