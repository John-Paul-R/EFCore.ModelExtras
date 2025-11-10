using System.Collections.Generic;
using System.Linq;
using Jp.Core.Extensions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Jp.Entities.Models.DbContext.Design;

public static class JpCustomMigrationsAnnotationProvider
{
    public static IEnumerable<IAnnotation> ForAdd(IForeignKeyConstraint property)
    {
        var annotations = new List<IAnnotation>();
        void AddIfFound(IAnnotation? annotation) {if (annotation is not null) annotations.Add(annotation);}

        var annotationsToFind = JpEfAnnotation.Key.GetAll();

        foreach (var annotationName in annotationsToFind)
        {
            var annotation = property.FindAnnotation(annotationName);
            if (annotation is not null) {
                annotations.Add(annotation);
            }
            property.MappedForeignKeys
                .Select(key => key.FindAnnotation(annotationName))
                .ForEach(AddIfFound);
        }

        return annotations.DistinctBy(a => a.Name);
    }
}
