Imports System.Text
Imports System.IO
Imports System.Security.Cryptography
Imports System.Web.Script.Serialization

Public Class EncryDecry
    Public Function Encrypt(ByVal encryptString As String) As String
        Dim EncryptionKey As String = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        Dim clearBytes As Byte() = Encoding.Unicode.GetBytes(encryptString)

        Using encryptor As Aes = Aes.Create()
            Dim pdb As Rfc2898DeriveBytes =
            New Rfc2898DeriveBytes(EncryptionKey,
                                   New Byte() {&H49, &H76, &H61, &H6E, &H20, &H4D, &H65, &H64, &H76, &H65, &H64, &H65, &H76})

            encryptor.Key = pdb.GetBytes(32)
            encryptor.IV = pdb.GetBytes(16)

            Using ms As New MemoryStream()
                Using cs As New CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write)
                    cs.Write(clearBytes, 0, clearBytes.Length)
                    cs.Close()
                End Using

                Return Convert.ToBase64String(ms.ToArray())
            End Using
        End Using
    End Function

    Public Function Encrypt(ByVal values As String()) As String
        Dim plainText As String = New JavaScriptSerializer().Serialize(values)
        Return encrypt(plainText)
    End Function

    Public Function Decrypt(ByVal cipherText As String) As String
        If String.IsNullOrEmpty(cipherText) Then
            Return Nothing
        End If

        Dim EncryptionKey As String = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        cipherText = cipherText.Replace(" ", "+")
        Dim cipherBytes As Byte() = Convert.FromBase64String(cipherText)

        Using encryptor As Aes = Aes.Create()
            Dim pdb As Rfc2898DeriveBytes =
            New Rfc2898DeriveBytes(EncryptionKey,
                                   New Byte() {&H49, &H76, &H61, &H6E, &H20, &H4D, &H65, &H64, &H76, &H65, &H64, &H65, &H76})

            encryptor.Key = pdb.GetBytes(32)
            encryptor.IV = pdb.GetBytes(16)

            Using ms As New MemoryStream()
                Using cs As New CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write)
                    cs.Write(cipherBytes, 0, cipherBytes.Length)
                    cs.Close()
                End Using

                Return Encoding.Unicode.GetString(ms.ToArray())
            End Using
        End Using
    End Function


    Public Function DecryptToArray(ByVal cipherText As String) As String()
        Dim json As String = Decrypt(cipherText)

        If String.IsNullOrEmpty(json) Then
            Return Nothing
        End If

        Return New JavaScriptSerializer().Deserialize(Of String())(json)
    End Function

    Public Function Encrypt(ByVal values As Dictionary(Of String, String)) As String
        If values Is Nothing Then
            Return Nothing
        End If

        Dim json As String = New JavaScriptSerializer().Serialize(values)
        Return Encrypt(json)
    End Function

    Public Function DecryptToDictionary(ByVal cipherText As String) As Dictionary(Of String, String)
        Dim json As String = Decrypt(cipherText)

        If String.IsNullOrEmpty(json) Then
            Return Nothing
        End If

        Return New JavaScriptSerializer().Deserialize(Of Dictionary(Of String, String))(json)
    End Function

    Public Function EncryptObject(Of T As Class)(ByVal obj As T) As String
        If obj Is Nothing Then
            Return Nothing
        End If

        Dim serializer As New JavaScriptSerializer()
        Dim json As String = serializer.Serialize(obj)
        Return Encrypt(json)
    End Function

    Public Function DecryptObject(Of T As Class)(ByVal cipherText As String) As T
        Dim json As String = Decrypt(cipherText)

        If String.IsNullOrEmpty(json) Then
            Return Nothing
        End If

        Dim serializer As New JavaScriptSerializer()
        Return serializer.Deserialize(Of T)(json)
    End Function

End Class

'example 
'Public Class PersonInfo
'    Public Property ID As String
'    Public Property Name As String
'    Public Property Phone As String
'End Class

'
'Dim p As New PersonInfo()
'p.ID = "1001"
'p.Name = "Ahmed"
'p.Phone = "123456"

'Dim encrypted As String = EncryptObject(Of PersonInfo)(p)