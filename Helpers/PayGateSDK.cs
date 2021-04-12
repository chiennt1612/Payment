using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Payment.Dtos;

namespace Payment.Helpers
{
    public class PayGateSDK
    {
        private string _merchant_id;
        private string _secret_key;
        private string _share_key;

        private string _merchant_private_key;

        private string _paygate_public_key;

        private string _url;

        /// <summary>
        /// khởi tạo PayGateSDK
        /// </summary>
        /// <param name="url">đường dẫn đến api của paygate</param>
        /// <param name="merchant_id">id duy nhất của merchant do paygate cung cấp</param>
        /// <param name="secret_key">khóa tham gia tạo chữ ký cho request gửi đến paygate do paygate cung cấp (salt)</param>
        /// <param name="share_key">khóa dùng để mã hóa data request gửi đến paygate do paygate cung cấp</param>
        /// <param name="merchant_private_key">khóa private của merchant, khi tích hợp paygate merchant sẽ tạo ra 1 cặp khóa rsa với độ dài của private key là 1024bit và 1 public key.
        /// Khóa private dùng để merchant tạo chữ ký gửi trong request đến paygate. Khóa public merchant sẽ cung cấp cho paygate, paygate dùng khó này để xác minh request có phải cho đúng merchant gửi hay không 
        /// </param>
        /// <param name="paygate_public_key">khóa công khai của paygate cung cấp cho merchant, khóa này dùng để kiểm tra chữ ký trong response của paygate trả về cho merchant với mục đích kiểm trả reponse trả về cho client là của paygate</param>
        public PayGateSDK(string url, string merchant_id, string secret_key, string share_key, string merchant_private_key, string paygate_public_key)
        {
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(merchant_id) || string.IsNullOrWhiteSpace(secret_key) || string.IsNullOrWhiteSpace(share_key)
                || string.IsNullOrWhiteSpace(merchant_private_key) || string.IsNullOrWhiteSpace(paygate_public_key))
                throw new ArgumentException();

            _url = url;
            _merchant_id = merchant_id;
            _secret_key = secret_key;
            _share_key = share_key;
            _merchant_private_key = merchant_private_key;
            _paygate_public_key = paygate_public_key;
        }

        /// <summary>
        /// lấy danh sách các service thanh toán paygate hổ trợ merchant thanh toán (url: /v1/getservices)
        /// </summary>
        /// <returns></returns>
        public async Task<PayGateResponse> GetServices()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_url);

                    //bước 1: tạo body request lấy service từ pagate bao gồm merchant code và chữ ký. Do lấy danh sách service không cần truyền tham số nên tham số để tao chữ ký sẽ là: _merchant_id + "" + _secret_key
                    var data = new
                    {
                        merchant_code = _merchant_id,
                        signature = MakeSign(_merchant_id + _secret_key)
                    };

                    //bước 2: gửi request data lên paygate trả về kết quả từ paygate
                    var response = await client.PostAsJsonAsync("/v1/getservices", data);

                    //kiểm tra http status code có = 200 hay không
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception($"Service return status code: {response.StatusCode}");

                    var responseData = await response.Content.ReadAsAsync<PayGateResponse>();

                    //kiểm tra code trả về từ paygate có thành công hay không ( = "0")
                    if (responseData.code != "0")
                        throw new Exception($"Service return code: {responseData.code}. {responseData.message}");

                    //giải mã data bằng _share_key
                    responseData.raw_data = AESDecryptData(responseData.data);

                    //kiểm tra chữ ký của reponse từ paygate có hợp lệ hay không
                    var isVerify = VerifySignature(responseData);

                    if (!isVerify)
                        throw new Exception($"Invalid signature");

                    return responseData;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// khởi tạo một giao dịch paygate (url: /v1/createpayment)
        /// </summary>
        /// <param name="service_code">code dịch vụ thanh toán, chọn 1 trong các dịch vụ được trả về từ hàm GetServices</param>
        /// <param name="order_id">mã đơn hàng của merchant, mã này là duy nhất</param>
        /// <param name="order_description">mô tả đơn hàng của merchant</param>
        /// <param name="amount">số tiền cần thanh toán</param>
        /// <param name="order_created_date">ngày tạo đơn hảng của merchant</param>
        /// <param name="return_url">url redirect sau khi thanh toán thành công</param>
        /// <param name="ip_address">địa chỉ ip của người thành toán</param>
        /// <returns></returns>
        public async Task<PayGateResponse> InitTransaction(int service_code, string order_id, string order_description, string amount, string order_created_date, string return_url, string ip_address)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_url);

                    //bước 1: tao một order info theo cấu trúc sau
                    var order_info = new
                    {
                        service_code = service_code,
                        currency_code = "VND",
                        txn_ref = order_id,
                        order_info = order_description,
                        ip_add = ip_address,
                        amount = amount,
                        return_url = return_url,
                        create_date = order_created_date
                    };

                    //bước 2: chuyển order_info sang dạng json
                    var order_info_json = JsonConvert.SerializeObject(order_info);

                    //bước 3:  mã hóa order_info json bằng thuật toán aes với khóa là share_key
                    var encrypt_data = AESEncryptData(order_info_json);

                    //bước 4: tạo chữ ký                     
                    var signature = MakeSign(_merchant_id + order_info_json + _secret_key);

                    //bước 5: tạo body cho request
                    var data = new
                    {
                        data = encrypt_data,
                        merchant_code = _merchant_id,
                        signature = signature
                    };

                    //bước 6: gửi request data lên paygate trả về kết quả từ paygate
                    var response = await client.PostAsJsonAsync("/v1/createpayment", data);

                    //kiểm tra http status code có = 200 hay không
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception($"Service return status code: {response.StatusCode}");

                    var responseData = await response.Content.ReadAsAsync<PayGateResponse>();

                    //kiểm tra code trả về từ paygate có thành công hay không ( = "0")
                    if (responseData.code != "0")
                        throw new Exception($"Service return code: {responseData.code}. {responseData.message}");

                    //giải mã data bằng _share_key
                    responseData.raw_data = AESDecryptData(responseData.data);

                    //kiểm tra chữ ký của reponse từ paygate có hợp lệ hay không
                    var isVerify = VerifySignature(responseData);

                    if (!isVerify)
                        throw new Exception($"Invalid signature");

                    return responseData;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// lấy thông tin giao dịch (url: /v1/checktransaction)
        /// </summary>
        /// <param name="order_id">là mã giao dịch đơn hàng của merchant</param>
        /// <param name="order_created_date">là ngày đơn hàng của merchant gửi cho paygate lúc khởi tạo giao dịch</param>
        /// <returns></returns>
        public async Task<PayGateResponse> GetTransaction(string order_id, string order_created_date)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(_url);

                    //bước 1: tao một object theo cấu trúc sau
                    var order_info = new
                    {
                        txn_ref = order_id,
                        txn_date = order_created_date
                    };

                    //bước 2: chuyển order_info sang dạng json
                    var order_info_json = JsonConvert.SerializeObject(order_info);

                    //bước 3:  mã hóa order_info json bằng thuật toán aes với khóa là share_key
                    var encrypt_data = AESEncryptData(order_info_json);

                    //bước 4: tạo chữ ký                     
                    var signature = MakeSign(_merchant_id + order_info_json + _secret_key);

                    //bước 5: tạo body cho request
                    var data = new
                    {
                        data = encrypt_data,
                        merchant_code = _merchant_id,
                        signature = signature
                    };

                    //bước 6: gửi request data lên paygate trả về kết quả từ paygate
                    var response = await client.PostAsJsonAsync("/v1/checktransaction", data);

                    //kiểm tra http status code có = 200 hay không
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception($"Service return status code: {response.StatusCode}");

                    var responseData = await response.Content.ReadAsAsync<PayGateResponse>();

                    //kiểm tra code trả về từ paygate có thành công hay không ( = "0")
                    if (responseData.code != "0")
                        throw new Exception($"Service return code: {responseData.code}. {responseData.message}");

                    //giải mã data bằng _share_key
                    responseData.raw_data = AESDecryptData(responseData.data);

                    //kiểm tra chữ ký của reponse từ paygate có hợp lệ hay không
                    var isVerify = VerifySignature(responseData);

                    if (!isVerify)
                        throw new Exception($"Invalid signature");

                    return responseData;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// hàm tạo chữ ký gửi trong request của paygate
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string MakeSign(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                throw new ArgumentException();

            //Bước 1: chuyển data thành dạng byte[]
            var bytesToEncrypt = Encoding.UTF8.GetBytes(data);

            //Bước 2: tạo chữ ký từ byte data (bytesToEncrypt) và merchant private key với thuật toán là sha1
            using (var rsaProvider = CreateRSACryptoServiceProviderFromPrivateKey())
            {
                var signatureBytes = rsaProvider.SignData(bytesToEncrypt, new SHA1CryptoServiceProvider());

                //Bước 3: chuyển signatureBytes sang dạng base64 string và trả về kết quả là chữ ký
                var signature = Convert.ToBase64String(signatureBytes);

                return signature;
            }
        }

        /// <summary>
        /// hàm kiểm tra chữ ký trong reponse từ paygate có hợp lệ hay không
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private bool VerifySignature(PayGateResponse response)
        {
            var data_sign = response.code + response.message + response.raw_data + _secret_key;

            using (var rsaProvider = CreateRSACryptoServiceProviderFromPublicKey())
            {
                return rsaProvider.VerifyData(Encoding.UTF8.GetBytes(data_sign), new SHA1CryptoServiceProvider(), Convert.FromBase64String(response.signature));
            }
        }

        public bool VerifySignature(string amount, string currency_code, string order_info, string pay_date, string service_name, string status, 
            string transaction_no, string txn_ref, string signature)
        {
            var data_sign = amount + currency_code + order_info + pay_date + service_name + status + transaction_no + txn_ref + _secret_key;

            using (var rsaProvider = CreateRSACryptoServiceProviderFromPublicKey())
            {
                return rsaProvider.VerifyData(Encoding.UTF8.GetBytes(data_sign), new SHA1CryptoServiceProvider(), Convert.FromBase64String(signature));
            }
        }

        /// <summary>
        /// Tạo RSA provider từ merchant private key
        /// </summary>
        /// <returns></returns>
        private RSACryptoServiceProvider CreateRSACryptoServiceProviderFromPrivateKey()
        {
            using (TextReader privateKeyTextReader = new StringReader(_merchant_private_key))
            {
                AsymmetricCipherKeyPair readKeyPair = (AsymmetricCipherKeyPair)new PemReader(privateKeyTextReader).ReadObject();
                RsaPrivateCrtKeyParameters privateKeyParams = ((RsaPrivateCrtKeyParameters)readKeyPair.Private);

                RSAParameters parms = new RSAParameters();
                parms.Modulus = privateKeyParams.Modulus.ToByteArrayUnsigned();
                parms.P = privateKeyParams.P.ToByteArrayUnsigned();
                parms.Q = privateKeyParams.Q.ToByteArrayUnsigned();
                parms.DP = privateKeyParams.DP.ToByteArrayUnsigned();
                parms.DQ = privateKeyParams.DQ.ToByteArrayUnsigned();
                parms.InverseQ = privateKeyParams.QInv.ToByteArrayUnsigned();
                parms.D = privateKeyParams.Exponent.ToByteArrayUnsigned();
                parms.Exponent = privateKeyParams.PublicExponent.ToByteArrayUnsigned();

                RSACryptoServiceProvider cryptoServiceProvider = new RSACryptoServiceProvider();
                cryptoServiceProvider.ImportParameters(parms);

                return cryptoServiceProvider;
            }
        }

        /// <summary>
        /// Tạo RSA provider từ paygate public key
        /// </summary>
        /// <returns></returns>
        public RSACryptoServiceProvider CreateRSACryptoServiceProviderFromPublicKey()
        {
            using (TextReader publicKeyTextReader = new StringReader(_paygate_public_key))
            {
                RsaKeyParameters publicKeyParam = (RsaKeyParameters)new PemReader(publicKeyTextReader).ReadObject();

                RSAParameters parms = new RSAParameters();
                parms.Modulus = publicKeyParam.Modulus.ToByteArrayUnsigned();
                parms.Exponent = publicKeyParam.Exponent.ToByteArrayUnsigned();

                RSACryptoServiceProvider cryptoServiceProvider = new RSACryptoServiceProvider();
                cryptoServiceProvider.ImportParameters(parms);

                return cryptoServiceProvider;
            }
        }

        private string AESDecryptData(string encryptedData)
        {
            //bước 1: chuyển encryptedData sang dạng byte
            var dataBytes = Convert.FromBase64String(encryptedData);

            //bước 2: chuyển share_key sang dạng byte
            var keyBytes = Convert.FromBase64String(_share_key);

            //bước 3: giải mã và trả về kết quả
            using (AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider())
            {
                using (MemoryStream ms = new MemoryStream(dataBytes))
                {
                    aesProvider.GenerateIV();
                    aesProvider.Mode = CipherMode.ECB;
                    aesProvider.Padding = PaddingMode.PKCS7;
                    aesProvider.Key = keyBytes;

                    using (CryptoStream cs = new CryptoStream(ms, aesProvider.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        byte[] result = new byte[dataBytes.Length];
                        cs.Read(result, 0, result.Length);
                        return Encoding.UTF8.GetString(result).TrimEnd('\0');
                    }
                }
            }
        }

        private string AESEncryptData(string data)
        {
            //bước 1: chuyển data sang dạng byte
            var dataBytes = Encoding.UTF8.GetBytes(data);

            //bước 2: chuyển share_key sang dạng byte
            var keyBytes = Convert.FromBase64String(_share_key);

            //bước 3: mã hóa và trả về kết quả
            using (AesCryptoServiceProvider aesProvider = new AesCryptoServiceProvider())
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    aesProvider.GenerateIV();
                    aesProvider.Mode = CipherMode.ECB;
                    aesProvider.Padding = PaddingMode.PKCS7;
                    aesProvider.Key = keyBytes;

                    using (CryptoStream cs = new CryptoStream(ms, aesProvider.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(dataBytes, 0, dataBytes.Length);
                        cs.FlushFinalBlock();
                        byte[] result = ms.ToArray();
                        return Convert.ToBase64String(result);
                    }
                }
            }
        }
    }
}
