# 9. Deployment as M365 Copilot Agent

## M365 Agents Toolkit and Enterprise Access Control

### Declarative Agent for M365 Copilot

The solution deploys as a **declarative agent** that integrates with M365 Copilot, making the Risk Agent available across the Microsoft 365 ecosystem:

- **M365 Copilot Integration**: Users access the agent directly from Copilot's interface
- **Microsoft Teams**: Available as a Teams app for chat-based interactions
- **Cross-Platform Access**: Works in Copilot on web, desktop, and mobile
- **Consistent Experience**: Same capabilities regardless of entry point

### M365 Agents Toolkit

The [M365 Agents Toolkit](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/overview-declarative-agent) simplifies agent deployment:

1. **Agent Manifest**: Define agent capabilities, instructions, and MCP tool connections
2. **App Package**: Bundle manifest with icons and metadata
3. **Teams App Catalog**: Deploy to organizational app store
4. **Copilot Discovery**: Agent appears in Copilot's agent picker

### Enterprise Security & Governance

**Identity & Access Control:**
- Microsoft Entra ID (Azure AD) authentication required
- Role-based access control (RBAC) for agent availability
- Conditional access policies apply automatically

**Data Protection:**
- All queries logged in M365 compliance center
- DLP policies enforced on agent responses
- Data residency follows tenant configuration

**Governance:**
- IT admins control agent deployment and availability
- Usage analytics and audit logs via M365 Admin Center
- Ability to restrict agent to specific users, groups, or departments

### Deployment Benefits

✅ **Zero Client Installation** - Works in existing Copilot/Teams clients  
✅ **Familiar Interface** - Users already know how to use Copilot  
✅ **Enterprise-Grade Security** - Inherits M365 security posture  
✅ **Centralized Management** - IT controls deployment and access  
✅ **Unified Experience** - Seamless alongside other Copilot capabilities

---

## Navigation

- [← Previous: Elastic MCP Integration](./08-elastic-mcp-integration.md)
- [Back to Demo Index](./README.md)
- [Next: Responsible AI & Content Safety →](./10-responsible-ai.md)
