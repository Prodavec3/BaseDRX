DECLARE @IdBoxBase varchar(50)
DECLARE boxBase_cursor CURSOR FOR   
SELECT Id from Sungero_ExCore_BoxBase 

OPEN boxBase_cursor  
FETCH NEXT FROM boxBase_cursor INTO @IdBoxBase 

	WHILE @@FETCH_STATUS = 0  
	BEGIN  
	if exists (select 1 from Sungero_Docflow_Params where [Key] = 'LastBoxMessageId_' + @IdBoxBase) and
	   not exists (select 1 from Sungero_Docflow_Params where [Key] = 'LastBoxIncomingMessageId_' + @IdBoxBase)
		begin
		  declare @LastId varchar(250)
		  set @LastId = (select [Value] from Sungero_Docflow_Params where [Key] = 'LastBoxMessageId_' + @IdBoxBase)
		  insert into Sungero_Docflow_Params values ('LastBoxIncomingMessageId_' + @IdBoxBase, @LastId)
		  delete from Sungero_Docflow_Params where [Key] = 'LastBoxMessageId_' + @IdBoxBase
		end
	FETCH NEXT FROM boxBase_cursor INTO @IdBoxBase  
	END  

CLOSE boxBase_cursor  
DEALLOCATE boxBase_cursor 