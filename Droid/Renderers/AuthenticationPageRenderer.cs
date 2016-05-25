﻿using Android.App;
using Newtonsoft.Json;
using System;
using System.Linq;
using TodoAWSSimpleDB;
using TodoAWSSimpleDB.Droid;
using Xamarin.Auth;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using customerapp;

[assembly: ExportRenderer(typeof(AuthenticationPage), typeof(AuthenticationPageRenderer))]

namespace TodoAWSSimpleDB.Droid
{
    // Use a custom page renderer to display the authentication UI on the AuthenticationPage
    public class AuthenticationPageRenderer : PageRenderer
    {
        bool isShown;

		RestService rest = new RestService();

        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);

            if (!App.IsLoggedIn)
            {
                if (!isShown)
                {
                    isShown = true;

                    // Initialize the object that communicates with the OAuth service
                    var auth = new OAuth2Authenticator(
                        Constants.ClientId,
                        Constants.ClientSecret,
                        Constants.Scope,
                        new Uri(Constants.AuthorizeUrl),
                        new Uri(Constants.RedirectUrl),
                        new Uri(Constants.AccessTokenUrl)
					);

                    auth.Completed += OnAuthenticationCompleted;

                    var activity = Context as Activity;
                    activity.StartActivity(auth.GetUI(activity));
                }
            }
            else
            {
                if (!isShown)
                {
                    App.SuccessfulLoginAction.Invoke();
                }
            }
        }

        async void OnAuthenticationCompleted(object sender, AuthenticatorCompletedEventArgs e)
        {
            if (e.IsAuthenticated)
            {
				Customer customer = await rest.GetCustomerInfo (e.Account);
				App.Customer = customer;
            }
            App.SuccessfulLoginAction.Invoke();
        }
    }
}
