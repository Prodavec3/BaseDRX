-- Если свойство Нерезидент не определено, сделать банк по умолчанию резидентом.
update Sungero_Parties_Counterparty
set Nonresident = 0
where Discriminator = '80c4e311-e95f-449b-984d-1fd540b8f0af'
  and Nonresident is null