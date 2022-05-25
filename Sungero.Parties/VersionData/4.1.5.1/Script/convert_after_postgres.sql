-- Если свойство Нерезидент не определено, сделать банк по умолчанию резидентом.
update sungero_parties_counterparty
set nonresident = false
where discriminator = '80c4e311-e95f-449b-984d-1fd540b8f0af'
  and nonresident is null