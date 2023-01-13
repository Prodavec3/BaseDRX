-- Заполнить свойство AllowApproveWithSuggestions значением по умолчанию.
-- bbb34726-4316-4d87-a64d-9dcf74b654f4 - GUID ApprovalStage
update Sungero_Docflow_ApprovalStage
set AllowApprWSugg = 'false'
where AllowApprWSugg IS NULL
and Discriminator = 'bbb34726-4316-4d87-a64d-9dcf74b654f4'