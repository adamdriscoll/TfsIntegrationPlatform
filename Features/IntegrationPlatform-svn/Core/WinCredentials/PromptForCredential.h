// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

//*****************************************************************************
//  MSDN Reference Link:	http://msdn.microsoft.com/library/aa480470.aspx
//***************************************************************************** 

#pragma once

namespace WinCredentials
{
    public ref class PromptForCredential
    {
    public:

        PromptForCredential();

        [SecurityPermission(SecurityAction::LinkDemand, Flags=SecurityPermissionFlag::UnmanagedCode)]
        ~PromptForCredential();

        property String^ TargetName
        {
            String^ get();
            void set(String^ value);
        }

        property int ErrorCode
        {
            int get();
            void set(int value);
        }

        property String^ UserName
        {
            String^ get();
            void set(String^ value);
        }

        property Security::SecureString^ Password
        {
            Security::SecureString^ get();
            void set(Security::SecureString^ value);
        }

        property bool SaveChecked
        {
            bool get();
            void set(bool value);
        }

        property String^ Message
        {
            String^ get();
            void set(String^ value);
        }

        property String^ Title
        {
            String^ get();
            void set(String^ value);
        }

        property Drawing::Bitmap^ Banner
        {
            Drawing::Bitmap^ get();
            void set(Drawing::Bitmap^ value);
        }

        property bool AlwaysShowUI
        {
            bool get();
            void set(bool value);
        }

        property bool CompleteUserName
        {
            bool get();
            void set(bool value);
        }

        property bool DoNotPersist
        {
            bool get();
            void set(bool value);
        }

        property bool ExcludeCertificates
        {
            bool get();
            void set(bool value);
        }

        property bool ExpectConfirmation
        {
            bool get();
            void set(bool value);
        }

        property bool GenericCredentials
        {
            bool get();
            void set(bool value);
        }

        property bool IncorrectPassword
        {
            bool get();
            void set(bool value);
        }

        property bool Persist
        {
            bool get();
            void set(bool value);
        }

        property bool RequestAdministrator
        {
            bool get();
            void set(bool value);
        }

        property bool RequireCertificate
        {
            bool get();
            void set(bool value);
        }

        property bool RequireSmartCard
        {
            bool get();
            void set(bool value);
        }

        property bool ShowSaveCheckBox
        {
            bool get();
            void set(bool value);
        }

        property bool UserNameReadOnly
        {
            bool get();
            void set(bool value);
        }

        property bool ValidateUserName
        {
            bool get();
            void set(bool value);
        }

        [SecurityPermission(SecurityAction::LinkDemand, Flags=SecurityPermissionFlag::UnmanagedCode)]
        Windows::Forms::DialogResult ShowDialog();

        [SecurityPermission(SecurityAction::LinkDemand, Flags=SecurityPermissionFlag::UnmanagedCode)]
        Windows::Forms::DialogResult ShowDialog(Windows::Forms::IWin32Window^ owner);

        [SecurityPermission(SecurityAction::LinkDemand, Flags=SecurityPermissionFlag::UnmanagedCode)]
        void ConfirmCredentials();

    private:

        void Flag(bool add,
                  DWORD flag);

        void CheckNotDisposed();

        bool m_disposed;

        String^ m_targetName;
        int m_errorCode;
        String^ m_userName;
        Security::SecureString^ m_password;
        bool m_saveChecked;
        String^ m_message;
        String^ m_title;
        Drawing::Bitmap^ m_banner;
        DWORD m_flags;

        bool m_confirmed;

    };
}
