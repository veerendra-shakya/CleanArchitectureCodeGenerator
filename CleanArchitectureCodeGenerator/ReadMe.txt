Rules for Entity Creation


1. Camel Case Naming for Ex. "ProductCode" for both Class and property naming
2. Space or '-' or '_'  are not allowed
3. Name should be singular
4. For Scafolding "public string? Name { get; set; }" is Required

5. Entity can inherit from "BaseAuditableSoftDeleteEntity", "BaseAuditableEntity", "BaseEntity", "IEntity", "ISoftDelete"

6. Known 





Recommendation:
If your main goal is to streamline scaffolding, defining basic validation attributes on your entities could be beneficial. However, for more complex scenarios, consider a hybrid approach where basic validations are in attributes and more advanced logic is in dedicated validators.