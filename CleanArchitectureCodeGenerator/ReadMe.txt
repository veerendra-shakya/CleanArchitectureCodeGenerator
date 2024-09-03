Rules for Entity Creation


1. Camel Case Naming for Ex. "ProductCode" for both Class and property naming
2. Space or '-' or '_'  are not allowed
3. Name should be singular
4. For Scafolding "public string? Name { get; set; }" is Required

5. Entity can inherit from "BaseAuditableSoftDeleteEntity", "BaseAuditableEntity", "BaseEntity", "IEntity", "ISoftDelete"

6. Known 


Supported Property Data Annotations are:  [Display(Name = "")], [Description("")].
Supported Property Validation Data Annotations are: [Required],  [MaxLength(100)], [Range(0, 100000)], [RegularExpression(@"^[a-zA-Z0-9]*$", ErrorMessage = "Only alphanumeric characters are allowed.")].


Recommendation:
If your main goal is to streamline scaffolding, defining basic validation attributes on your entities could be beneficial. However, for more complex scenarios, consider a hybrid approach where basic validations are in attributes and more advanced logic is in dedicated validators.