do $$
declare 
	IdBoxBase varchar;
	LastId varchar;
begin
FOR IdBoxBase IN SELECT Id from Sungero_ExCore_BoxBase  LOOP
	if exists (select 1 from Sungero_Docflow_Params where Key = 'LastBoxMessageId_' || IdBoxBase) and
		not exists (select 1 from Sungero_Docflow_Params where Key = 'LastBoxIncomingMessageId_' || IdBoxBase)
	then
		  LastId := (select Value from Sungero_Docflow_Params where Key = 'LastBoxMessageId_' || IdBoxBase);
		  insert into Sungero_Docflow_Params values ('LastBoxIncomingMessageId_' || IdBoxBase, LastId);
		  delete from Sungero_Docflow_Params where Key = 'LastBoxMessageId_' || IdBoxBase;
	end if;
END LOOP;

end$$;