SELECT 
       T0.DocEntry
     , T0.DocNum
     , T0.DocDate
     , T1.Code [TransTypeCode]
     , T1.Name [TransTypeName]
     , T2.AcctCode
     , T2.AcctName
     , T0.U_PURNo [PurNo]
     , T0.U_TransType [TransType]
     , T0.U_WARNo [WarNo]
     , T0.U_BpCode [CardCode]
     , T0.U_BPName [CardName]
     , T0.U_PrepBy [PreparedBy]
     , T0.U_AppBy [ApprovedBy]
     , T0.U_Remarks [DocRemarks]
     , T0.U_NotedBy [NotedBy]
     , T0.U_RecBy [ReceivedBy]
     , T0.U_Desig [Designation]
FROM OIGN AS T0
LEFT JOIN [@TRANSACTION_TYPE] T1 ON T1.Code = T0.U_TransType
LEFT JOIN OACT T2 ON T2.FormatCode = T1.U_AcctCode
WHERE T0.DocEntry = @DocEntry