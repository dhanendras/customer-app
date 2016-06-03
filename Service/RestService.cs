﻿using System;
using System.Net.Http;
using System.Collections.Generic;
using customerapp.Dto;
using Newtonsoft.Json;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

using Xamarin.Forms;

using Plugin.Geolocator;
using Xamarin.Auth;
using Newtonsoft.Json.Converters;
using Acr.UserDialogs;

namespace customerapp
{
	public class RestService : IRestService
	{
		HttpClient client;

		/**
		 * We need this custom setting, so that JSON is created with 
		 * correct propertyName. Without this setting, json Name would be start 
		 * with capital letter as in .Net Property. With this setting
		 * all property names in json start with small letter.
		 */
		JsonSerializerSettings jsonSetting = new JsonSerializerSettings { 
			ContractResolver = new CamelCasePropertyNamesContractResolver(),
			NullValueHandling = NullValueHandling.Ignore // ignore null values
		};
				
		public RestService ()
		{
			client = new HttpClient ();
            // 30s is reasonable for timeout
            client.Timeout = TimeSpan.FromSeconds(30);
		}

        public async Task<List<Product>> GetAllProductsByLocation (Position customerPosition)
		{
            UserDialogs.Instance.ShowLoading ("Loading available products");

            string x = customerPosition.Lat.ToString();
            string y = customerPosition.Lon.ToString();

            // replace is needed to be sure, that double is written xx.zz not xx,zz
            var response = await DoGetRequestWithErrorHandling (
                Constants.ApiUrlListProducts, 
                x.Replace(',', '.'), 
                y.Replace(',', '.')
            );

            if (response != null && response.IsSuccessStatusCode) {
				var content = await response.Content.ReadAsStringAsync ();

                UserDialogs.Instance.HideLoading ();
				return Newtonsoft.Json.JsonConvert.DeserializeObject <List<Product>> (content);
			} else {
                return new List<Product>();
			}
		}

		public async Task<List<Order>> GetAllOrders (String customerId)
		{
            UserDialogs.Instance.ShowLoading ("Loading all orders");
			Debug.WriteLine ("Fetch all orders for customer {0} ", customerId);

            var response = await DoGetRequestWithErrorHandling (Constants.ApiUrlOrdersByCustomer, customerId);
			List<Order> items = new List<Order>();

			if (response.IsSuccessStatusCode) {
				var content = await response.Content.ReadAsStringAsync ();
				items = Newtonsoft.Json.JsonConvert.DeserializeObject <List<Order>> (content, new JavaScriptDateTimeConverter ());
			} else {
				Debug.WriteLine ("Failed to get all orders with status code " + response.StatusCode);
			}

			return items;
		}

		public async Task<List<Mission>> GetAllMissions (String orderId)
		{
			Debug.WriteLine ("Fetch all missions for {0} ", orderId);

			var uri = new Uri (String.Format(Constants.ApiUrlMissionsByOrder, orderId));
			Debug.WriteLine ("URI " + uri);

			var response = await client.GetAsync (uri);
			List<Mission> items = new List<Mission>();

			if (response.IsSuccessStatusCode) {
				var content = await response.Content.ReadAsStringAsync ();
				items = Newtonsoft.Json.JsonConvert.DeserializeObject <List<Mission>> (content, jsonSetting);
			} else {
				Debug.WriteLine ("Failed to get all missions with status code " + response.StatusCode);
			}

			return items;
		}

		public async Task ConfirmOrder(String orderId, String customerId){
			var uri = new Uri (string.Format(Constants.ApiUrlConfirmOrder, orderId, customerId));
			Debug.WriteLine ("URI " + uri);
			var content = new StringContent ("", Encoding.UTF8);
			HttpResponseMessage response = await client.PostAsync (uri, content);

			if (!response.IsSuccessStatusCode) {
				Debug.WriteLine (
					"Failed to confirm order {0} for customer {1} with status code {2}", 
					response.StatusCode, 
					orderId, 
					customerId
				);
			}
		
		}

        public async Task<Order> CreateOrder (ICollection<Product> orderProducts, Position customerPosition)
		{
            UserDialogs.Instance.ShowLoading ("Getting drop position");
			var projectId = getProjectId (orderProducts);

			var request = new {
                customerPosition = customerPosition,
				projectId = projectId,
				orderProducts = orderProducts
			};
                    
            var uri = new Uri (Constants.ApiUrlListOrder);
            var response = await DoPostRequestWithErrorHandling(uri, request);

			if (response != null && response.IsSuccessStatusCode) {
				var content = await response.Content.ReadAsStringAsync();

                UserDialogs.Instance.HideLoading ();
				return Newtonsoft.Json.JsonConvert.DeserializeObject <Order> (content, jsonSetting); 
			} else {
				Debug.WriteLine ("Failed to create order with status code " + response.StatusCode);
				return null;
			}
		}

		public async Task DeleteOrder (string orderId)
		{
			var uri = new Uri (string.Format(Constants.ApiUrlDeleteOrder, orderId));
			Debug.WriteLine ("URI " + uri);

			var content = new StringContent ("", Encoding.UTF8);
			HttpResponseMessage response = await client.PostAsync (uri, content);
			Debug.WriteLine ("Delete order {0} successfully", orderId);

			if (!response.IsSuccessStatusCode) {
				Debug.WriteLine ("Failed to delete order {0} with status code {1} ", orderId, response.StatusCode);
			}
		}

		public async Task<Customer> GetCustomerInfo (Account account)
		{
			// If the user is authenticated, request their basic user data from Google
			var request = new OAuth2Request("GET", new Uri(Constants.UserInfoUrl), null, account);

			var response = await request.GetResponseAsync();
			if (response != null)
			{
				// Deserialize the data and store it in the account store
				string userJson = response.GetResponseText();

				CustomerGoogleDto googleCustomer = JsonConvert.DeserializeObject<CustomerGoogleDto>(userJson, jsonSetting);
				return new Customer {
					FamilyName = googleCustomer.FamilyName,
					GivenName = googleCustomer.GivenName,
					Email = googleCustomer.Email,
				};
			}

			return null;
		}

		public async Task<Customer> SaveCustomer (Customer customer)
		{
			var uri = new Uri (Constants.ApiUrlSaveCustomer);
			Debug.WriteLine ("URI " + uri);

			var jsonToSend = JsonConvert.SerializeObject (customer, jsonSetting);
			var contentToSend = new StringContent (jsonToSend, Encoding.UTF8, "application/json");

			HttpResponseMessage response = await client.PostAsync (uri, contentToSend);

			if (response.IsSuccessStatusCode) {
				var content = await response.Content.ReadAsStringAsync();
				Customer output = Newtonsoft.Json.JsonConvert.DeserializeObject <Customer> (content, jsonSetting); 
				return output;
			} else {
				Debug.WriteLine ("Failed to save customer with status code " + response.StatusCode);
				return null;
			}
		}


		public async Task<Customer> GetCustomerById (String customerId)
		{
			Debug.WriteLine ("Fetch customer for id {0}", customerId);

			var uri = new Uri (String.Format(Constants.ApiUrlCustomerFind, customerId));
			Debug.WriteLine ("URI " + uri);

			var response = await client.GetAsync (uri);
			if (response.IsSuccessStatusCode) {
				var content = await response.Content.ReadAsStringAsync ();
				return Newtonsoft.Json.JsonConvert.DeserializeObject <Customer> (content);
			} else {
				Debug.WriteLine ("Failed to get customer " + customerId + " with status code " + response.StatusCode);
			}

			return null;
		}

        private async Task<HttpResponseMessage> DoGetRequestWithErrorHandling(string urlWithParam, params object[] param){
            var uri = new Uri (String.Format(urlWithParam, param));

            try{
                
                var response = await client.GetAsync (uri);

                if(!response.IsSuccessStatusCode){
                    Debug.WriteLine ("Request Failed to url {0} and response code {1}", uri, response.StatusCode);
                    UserDialogs.Instance.ShowError ("Failed to reach server.");
                    return null;
                }else{
                    return response;    
                }
            } catch(Exception ex){
                UserDialogs.Instance.ShowError ("Failed to reach server.");
                Debug.WriteLine ("Request Failed to url {0}", uri, ex);
                return null;
            }
        }


        private async Task<HttpResponseMessage> DoPostRequestWithErrorHandling(Uri uri, object content){

            try{
                var jsonToSend = JsonConvert.SerializeObject (content, jsonSetting);
                var contentToSend = new StringContent (jsonToSend, Encoding.UTF8, "application/json");

                var response = await client.PostAsync (uri, contentToSend);

                if(!response.IsSuccessStatusCode){
                    Debug.WriteLine ("Request Failed to url {0} and response code {1}", uri, response.StatusCode);
                    UserDialogs.Instance.ShowError ("Failed to reach server.");
                    return null;
                }else{
                    return response;    
                }
            } catch(Exception ex){
                UserDialogs.Instance.ShowError ("Failed to reach server.");
                Debug.WriteLine ("Request Failed to url {0}", uri, ex);
                return null;
            }
        }


        private String getProjectId(ICollection<Product> orderProducts){
            /**
             * Since all products should be from the same project
             * we can get the project id from the first product
             */
            var enumerator = orderProducts.GetEnumerator ();
            enumerator.MoveNext ();
            return enumerator.Current.ProjectId;
        }

	}

}

