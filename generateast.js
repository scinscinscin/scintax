const fs = require("fs");
const tree = require("./nodes.json");
const files = Object.keys(tree);

for(const file of files){
	const subclasses = tree[file]["subclasses"].map((str) => {
		const spl = str.split(" : ");
		return { name: spl[0], fields: spl[1].split(", ") };
	});
	
	//console.log("Here are the subclasses", subclasses);
	let output = "using System;\n\n";
	
	const generic_parameter = tree[file].is_generic ? "<T>" : "";
	const generic = tree[file].is_generic ? "T" : "void";

	// generate the base interface for the ast visitor
	output += `interface ${file}Visitor${generic_parameter}{\n`
	for(const { name } of subclasses) 
		output += `	${generic} visit${name}${file}(${name}${file} ${file.toLowerCase()});\n`;
	output += `}\n\n`

	// generate the base class
	output += `abstract class ${file}{\n`;
	output += `	public abstract ${generic} accept${generic_parameter}(${file}Visitor${generic_parameter} visitor);\n`
	output += `}\n\n`

	// generate the subclasses
	for(const subcl of subclasses){
		output += `class ${subcl.name}${file} : ${file}{\n`;
		for(const field of subcl.fields) output += `	public readonly ${field};\n`;
		
		// generate the constructor of the subclass
		output += `\n	public ${subcl.name}${file}(${subcl.fields.join(", ")}){\n`;
		for(const field of subcl.fields){
			const fieldname = field.split(" ")[1];
			output += `		this.${fieldname} = ${fieldname};\n`;
		};
		output += `	}\n`
		
		// add the visitor reflect
		output += `	public override ${generic} accept${generic_parameter}(${file}Visitor${generic_parameter} visitor){ ${tree[file].is_generic ? "return " : ""}visitor.visit${subcl.name}${file}(this); }\n`
		output += `}\n\n`;
	}

	//console.log(output);
	fs.writeFileSync(`${file}.cs`, output, { encoding: "utf-8" });
}
