// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    using System.IO;
    using System.Threading;
    using System.Security;
    using System.Security.Util;
    using System.Security.Permissions;
    using System.Runtime.CompilerServices;
    using System.Collections;
    using System.Text;
    using System;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using IUnrestrictedPermission = System.Security.Permissions.IUnrestrictedPermission;

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    internal abstract class CodeAccessPermission
        : IPermission
    {
        // Static methods for manipulation of stack
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static void RevertAssert()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.RevertAssert(ref stackMark);
        }

        //
        // Standard implementation of IPermission methods for
        // code-access permissions.
        //

        // Mark this method as requiring a security object on the caller's frame
        // so the caller won't be inlined (which would mess up stack crawling).
        [DynamicSecurityMethodAttribute()]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public void Demand()
        {
            if (!this.CheckDemand( null ))
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMyCallersCaller;
                CodeAccessSecurityEngine.Check(this, ref stackMark);
            }
        }

        // Metadata for this method should be flaged with REQ_SQ so that
        // EE can allocate space on the stack frame for FrameSecurityDescriptor

        [DynamicSecurityMethodAttribute()]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public void Assert()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            CodeAccessSecurityEngine.Assert(this, ref stackMark);
        }

        // IPermission interfaces

        // We provide a default implementation of Union here.
        // Any permission that doesn't provide its own representation 
        // of Union will get this one and trigger CompoundPermission
        // We can take care of simple cases here...

        public virtual IPermission Union(IPermission other) {
            // The other guy could be null
            if (other == null) return(this.Copy());
            
            // otherwise we don't support it.
            throw new NotSupportedException(Environment.GetResourceString( "NotSupported_SecurityPermissionUnion" ));
        }

        //
        // HELPERS FOR IMPLEMENTING ABSTRACT METHODS
        //

        //
        // Protected helper
        //

        internal bool VerifyType(IPermission perm)
        {
            // if perm is null, then obviously not of the same type
            if ((perm == null) || (perm.GetType() != this.GetType())) {
                return(false);
            } else {
                return(true);
            }
        }

        // The IPermission Interface
        public abstract IPermission Copy();
        public abstract IPermission Intersect(IPermission target);
        public abstract bool IsSubsetOf(IPermission target);

        [System.Runtime.InteropServices.ComVisible(false)]
        public override bool Equals(Object obj)
        {
            IPermission perm = obj as IPermission;
            if(obj != null && perm == null)
                return false;
            try {
                if(!this.IsSubsetOf(perm))
                    return false;
                if(perm != null && !perm.IsSubsetOf(this))
                    return false;
            }
            catch (ArgumentException)
            {
                // Any argument exception implies inequality
                // Note that we require a try/catch block here because we have to deal with
                // custom permissions that may throw exceptions indiscriminately.
                return false;
            }
            return true;
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public override int GetHashCode()
        {
            // This implementation is only to silence a compiler warning.
            return base.GetHashCode();
        }


        internal bool CheckDemand(CodeAccessPermission grant)
        {
            Debug.Assert( grant == null || grant.GetType().Equals( this.GetType() ), "CheckDemand not defined for permissions of different type" );
            return IsSubsetOf( grant );
        }

        internal bool CheckPermitOnly(CodeAccessPermission permitted)
        {
            Debug.Assert( permitted == null || permitted.GetType().Equals( this.GetType() ), "CheckPermitOnly not defined for permissions of different type" );
            return IsSubsetOf( permitted );
        }

        internal bool CheckDeny(CodeAccessPermission denied)
        {
            Debug.Assert( denied == null || denied.GetType().Equals( this.GetType() ), "CheckDeny not defined for permissions of different type" );
            IPermission intersectPerm = Intersect(denied);
            return (intersectPerm == null || intersectPerm.IsSubsetOf(null));
        }

        internal bool CheckAssert(CodeAccessPermission asserted)
        {
            Debug.Assert( asserted == null || asserted.GetType().Equals( this.GetType() ), "CheckPermitOnly not defined for permissions of different type" );
            return IsSubsetOf( asserted );
        }
    }
}
