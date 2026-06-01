SELECT 
       T0.DocEntry
     , T0.DocNum
     , T0.DocDate
     , T0.U_PrepBy [PreparedBy]
     , T1.Code [TransTypeCode]
     , T1.Name [TransTypeName]
     , T2.AcctCode
     , T2.AcctName
FROM OIGE AS T0
LEFT JOIN [@TRANSACTION_TYPE] T1 ON T1.Code = T0.U_TransType
LEFT JOIN OACT T2 ON T2.FormatCode = T1.U_AcctCode
WHERE T0.Comments LIKE '%WMS%'