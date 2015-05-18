# Copyright Henrik Feldt 2012
require 'albacore' # gem install albacore
require 'fileutils' #in ruby core
require 'semver' #gem install semver2
require_relative 'build_support/environment'

def conf_assert
  raise "You have to call ':release' or ':debug' to run this task" unless ENV['CONFIGURATION']
end

desc "Update the common version information for the build. You can call this task without building."
assemblyinfo :global_version => [:versioning] do |asm|
  # Assembly file config
  asm.product_name = 'MassTransit.Persistence.RavenDb'
  asm.version = FORMAL_VERSION
  asm.file_version = FORMAL_VERSION
  asm.custom_attributes :AssemblyInformationalVersion => "#{BUILD_VERSION}",
    :ComVisibleAttribute => false,
    :CLSCompliantAttribute => false
  asm.copyright = 'Peter Davis, Chris Patterson 2013'
  asm.output_file = 'src/SolutionVersion.cs'
  asm.namespaces "System", "System.Reflection", "System.Runtime.InteropServices", "System.Security"
end

desc "Ensure that all NuGet packages are here"
task :ensure_packages do    
  sh %Q[NuGet.exe restore "src/MassTransit.Persistence.RavenDB.sln"] do |ok, res| 
    puts (res.inspect) unless ok
  end

end

desc "Compile Solution"
msbuild :compile => [:ensure_packages, :global_version] do |msb|
  msb.solution = 'src/MassTransit.Persistence.RavenDB.sln'
  msb.properties :Configuration => CONFIGURATION
  msb.targets    :Rebuild
  msb.verbosity = "minimal"
end

desc "Run Tests"
nunit :test => [:release, :compile] do |n|
  conf_assert
  asms = Dir.glob("#{File.dirname(__FILE__)}/src/MassTransit.*.Tests/bin/#{CONFIGURATION}/*.Tests.dll")
  puts "Running nunit with assemblies: #{asms.inspect}"
  n.command = Dir.glob("#{File.dirname(__FILE__)}/src/packages/NUnit*/Tools/nunit-console.exe").first
  n.assemblies = asms 
  n.parameters = ['/framework=net-4.0']
end

desc "Compile Solution, Run Tests"
task :default => [:release, :compile, :test]

task :nuspec_copy do
  conf_assert
  FileList[File.join('src', "*/bin", CONFIGURATION, "MassTransit.*.{dll,xml}")].keep_if{ |f|
    ff = f.downcase
    !(ff.include?("test") || ff.include?("msmq"))
  }.each { |f| 
    to = File.join( 'build/nuspec', 'lib', FRAMEWORK )
    FileUtils.mkdir_p to
    cp f, to
    File.join(FRAMEWORK, File.basename(f))
  }
end

directory 'build/nuspec'

desc "Create a nuspec for 'MassTransit.Persistence.RavenDB'"
nuspec :nuspec => ['build/nuspec', :nuspec_copy] do |nuspec|
  conf_assert
  nuspec.id = "MassTransit.Persistence.RavenDB"
  nuspec.version = NUGET_VERSION
  nuspec.authors = ["Dru Sellers", "MPS Broadband", "Chris Patterson", "Peter Davis"]
  nuspec.owners = ["phatboyg"]
  nuspec.description = "MassTransit persistence library for RavenDB."
  nuspec.title = "MassTransit RavenDB Persistence"
  nuspec.project_url = "https://github.com/MassTransit/MassTransit-RavenDb"
  nuspec.language = "en-GB"
  nuspec.license_url = "http://www.apache.org/licenses/LICENSE-2.0"
  nuspec.dependency "MassTransit", "2.9.9"
  nuspec.dependency "Newtonsoft.Json", "6.0.6"
  nuspec.dependency "RavenDB.Client", "2.5.2956"
  nuspec.output_file = 'build/nuspec/MassTransit.Persistence.RavenDB.nuspec'
end

directory 'build/nuget'

desc "nuget pack 'MassTransit.Persistence.RavenDB'"
nugetpack :nuget => ['build/nuget', :release, :nuspec] do |nuget|
  conf_assert
  nuget.command     = 'NuGet.exe'
  nuget.nuspec      = 'build/nuspec/MassTransit.Persistence.RavenDB.nuspec'
  nuget.output_directory = 'build/nuget'
end

desc "publishes (pushes) the nuget package 'MassTransit.Persistence.RavenDB'"
nugetpush :nuget_push => [:release, :versioning] do |nuget|
  nuget.command = 'src/.nuget/NuGet.exe'
  nuget.package = File.join("build/nuget", 'MassTransit.Persistence.RavenDB' + "." + BUILD_VERSION + '.nupkg')
end

desc "publish nugets! (doesn't build)"
task :publish => [:verify, :default, :git, :nuget_push]

task :verify do
  changed_files = `git diff --cached --name-only`.split("\n") + `git diff --name-only`.split("\n")
  if !(changed_files == [".semver", "Rakefile.rb"] or 
    changed_files == ["Rakefile.rb"] or 
	changed_files == [".semver"] or
    changed_files.empty?)
    raise "Repository contains uncommitted changes; either commit or stash."
  end
end

task :git do 
  v = SemVer.find
  if `git tag`.split("\n").include?("#{v.to_s}")
    raise "Version #{v.to_s} has already been released! You cannot release it twice."
  end
  puts 'committing'
  `git commit -am "Released version #{v.to_s}"` 
  puts 'tagging'
  `git tag #{v.to_s}`
  puts 'pushing'
  `git push`
  `git push --tags`
end

desc "Perform a Full-on Release!"
task :everything => [:verify, :default, :git, :publish] do
  puts 'done'
end