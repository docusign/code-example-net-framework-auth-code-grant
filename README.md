# NetFrameworkOAuth
 
This code example demonstrates how to use the OAuth Authorization Code Grant flow from a .NET Framework MVC application

## Configuration

### Create the Integration Key
Log in to your DocuSign developer account. If you don’t have an account, you can [create a developer account for free](https://go.docusign.com/o/sandbox/?ga=2.70927056.1363819232.1590515244-192278368.1546193875&ECID=20890&elqCampaignId=20890&LS=NA_DEV_BOTH_BetaSite_2020-05&utm_campaign=NA_DEV_BOTH_BetaSite_2020-05&Channel=DDCUS000016968056&cName=DocuSign.com&postActivateUrl=https://developers.docusign.com/docs/esign-rest-api/quickstart/). From your account menu on the Developer Center, choose [My Apps and Keys](https://admindemo.docusign.com/authenticate?goTo=apiIntegratorKey) and then select the ADD APP AND INTEGRATION KEY button. Name your app, and DocuSign will generate an integration key for it and open the app’s configuration page. 
Set the integration key to use the Authorization Code Grant flow.
Add a secret key to the app. Secret keys are only shown once, when they’re created; so as soon as you add it, copy the key’s value and store it securely elsewhere. However, if you lose the value for this secret key, you can add a new one. Your app can have as many secret keys as you wish.
Add a redirect URI to the integration key for the URL. This should fit the pattern  code_example_url/ds/callback. The default URL for the application is https://localhost:44359. Therefore, the default redirect URI to be added to the integration key is: https://localhost:44359/ds/callback
Update Project Url to https://localhost:44359/

### Update the Web.config settings file
You now have two mandatory items and one optional item to add to the code example's Web.config file, which is at the top level of the code example's directory:

* The **integration key**: add it as the **ClientId** setting.
* The **secret value**: add it as the **SecretKey** setting.
* The **preferred account ID**: add it as the **RequiredAccount** optional setting.

## Using the code example

Run **git clone https://github.com/docusign/code-example-net-framework-auth-code-grant.git** 

Build and run the application in Visual Studio. 

VS will start an instance of IIS as the application's web server.VS will open your default browser to the application.
Use the Authenticate with DocuSign option in the top navigation pane to start the authentication process. The browser will be redirected to the DocuSign Identity Provider (IdP) for the authentication and consent process. The browser is then redirected back to the application to complete the Authorization Code Grant flow.
Use the Example option in the top navigation pane to send an envelope.

Use the **Example** option in the top navigation pane to send an 
envelope.

## License and additional information

### License
This repository uses the MIT License. Please see the LICENSE file for more information.

### Pull Requests
Pull requests are welcomed. Pull requests will only be considered if their content
uses the MIT License.

### Additional Resources
* [DocuSign Developer Center](https://developers.docusign.com)
* [DocuSign API on Twitter](https://twitter.com/docusignapi)
* [DocuSign For Developers on LinkedIn](https://www.linkedin.com/showcase/docusign-for-developers/)
* [DocuSign For Developers on YouTube](https://www.youtube.com/channel/UCJSJ2kMs_qeQotmw4-lX2NQ)
