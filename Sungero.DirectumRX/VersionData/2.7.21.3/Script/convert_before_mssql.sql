-- Восстановление типа спец. папок.
if exists (select * from information_schema.tables where table_name = 'SpecialFolders')
  drop table SpecialFolders
  
create table SpecialFolders(FolderName nvarchar(250), MainEntityType uniqueidentifier)
insert into SpecialFolders values ('825bcdaa-f676-490f-9ce8-3c95355a63d4','59079f7f-a326-4947-bbd6-0ae6dfb5f59b') -- ContractsList
insert into SpecialFolders values ('f7a4bbcf-69d8-49c5-a86e-274fa1b1ce5f','59079f7f-a326-4947-bbd6-0ae6dfb5f59b') -- ExpiringSoonContracts
insert into SpecialFolders values ('7afd9b8e-f293-4984-a75e-c09f44b598a7','58cca102-1e97-4f07-b6ac-fd866a8b7cb1') -- ContractsAtContractors
insert into SpecialFolders values ('c09c7eeb-5957-4631-be80-a589b8cfbec5','59079f7f-a326-4947-bbd6-0ae6dfb5f59b') -- ContractsHistory
insert into SpecialFolders values ('37d0e546-5b38-4b60-b680-5ab862dcea47','58cca102-1e97-4f07-b6ac-fd866a8b7cb1') -- IssuanceJournal
insert into SpecialFolders values ('113427f5-1683-4bb8-980c-ba4bcef50e66','42a6a084-6828-47d9-95bb-50b0538a6037') -- ApprovalRules
insert into SpecialFolders values ('727ee230-9497-48cd-bceb-096fe7fc9a63','59079f7f-a326-4947-bbd6-0ae6dfb5f59b') -- FinContractList
insert into SpecialFolders values ('7dd47bdf-ac4b-4919-a6f1-4ed67d7e086a','96c4f4f3-dc74-497a-b347-e8faf4afe320') -- DocumentsWithoutScan
insert into SpecialFolders values ('0e79392a-07bb-42e8-85fa-3be38d8a4cc5','96c4f4f3-dc74-497a-b347-e8faf4afe320') -- SignAwaitedDocuments
insert into SpecialFolders values ('1f0ee6ab-06cb-4f66-bf2d-97abb3ea1a43','294767f1-009f-4fbd-80fc-f98c49ddc560') -- BlockedCounterparties
insert into SpecialFolders values ('87d25407-dbad-46a4-927c-34d0e11562dc','294767f1-009f-4fbd-80fc-f98c49ddc560') -- InvitedCounterparties
insert into SpecialFolders values ('81fa363f-766c-423b-818a-e48efe57c3d4','58cca102-1e97-4f07-b6ac-fd866a8b7cb1') -- ProjectDocuments
insert into SpecialFolders values ('c21fa108-1570-4155-8a0c-0d04957bc72b','ea683a63-273e-43ae-bcf1-7a443698008a') -- ForExecution
insert into SpecialFolders values ('4b30b2be-330e-42bd-833c-7ecf19b32e12','d795d1f6-45c1-4e5e-9677-b53fb7280c7e') -- ActionItems
insert into SpecialFolders values ('bd7ced8f-379e-4cda-9f18-a28193b8d53b','58cca102-1e97-4f07-b6ac-fd866a8b7cb1') -- DocumentsToReturn
insert into SpecialFolders values ('0d61168d-1ae5-4570-a42e-bdd5f3b07b02','ea683a63-273e-43ae-bcf1-7a443698008a') -- OnReview
insert into SpecialFolders values ('669ff039-2f05-4eb3-9d6a-049dafee948f','ea683a63-273e-43ae-bcf1-7a443698008a') -- OnSigning
insert into SpecialFolders values ('1ead1abe-1b7e-4833-9e01-ee7f8404537a','ea683a63-273e-43ae-bcf1-7a443698008a') -- OnDocumentProcessing
insert into SpecialFolders values ('852ea6a2-304e-48d1-88e5-1bcbb1fb69a2','ea683a63-273e-43ae-bcf1-7a443698008a') -- OnApproval
insert into SpecialFolders values ('a353a121-f96e-4842-8b80-bf77d074a0c2','ea683a63-273e-43ae-bcf1-7a443698008a') -- OnCheking
insert into SpecialFolders values ('35d23c5e-9106-4068-bc3a-46b471928fa0','ea683a63-273e-43ae-bcf1-7a443698008a') -- OnPrint
insert into SpecialFolders values ('a584ebf5-f9b3-4e41-b7cf-15d8c2f96844','ea683a63-273e-43ae-bcf1-7a443698008a') -- OnRework
insert into SpecialFolders values ('aee5f0a0-8a5f-422f-bc40-e98db38e94ca','ea683a63-273e-43ae-bcf1-7a443698008a') -- OnRegister
insert into SpecialFolders values ('ae3b3ff0-73ac-4590-961c-adf4579d59de','ef79164b-2ce7-451b-9ba6-eb59dd9a4a74') -- Notices
insert into SpecialFolders values ('1440d1dc-6746-434a-908d-e596c2a1f849','d795d1f6-45c1-4e5e-9677-b53fb7280c7e') -- Approval
insert into SpecialFolders values ('1031e40e-2c3a-4964-92ce-92036cdcb3b9','ea683a63-273e-43ae-bcf1-7a443698008a') -- ExchangeDocumentProcessing
  
update f
  set MainEntityType = sf.MainEntityType
  from [Sungero_Core_Folder] f
  join [SpecialFolders] sf
    on f.Name = sf.FolderName
  where f.MainEntityType <> sf.MainEntityType
  
drop table SpecialFolders